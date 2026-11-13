// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.Text;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using osu.Game.Ai;

namespace hotia.AiMapper.Inference
{
    public sealed class InferencePipeline
    {
        private const int sample_rate = 16000;
        private const int window_samples = 262016;
        private const int maximum_tokens = 2560;

        private const double lookback = 0.2; //0.5;
        private const double lookahead = 0.2; //0.4;
        private const double window_ms =
            window_samples * 1000.0 / sample_rate;

        private static readonly int stride_samples = (int)(window_samples * (1.0 - lookback - lookahead));

        private readonly InferenceSession encoder;
        private readonly InferenceSession prefill;
        private readonly InferenceSession step;
        private readonly Tokenizer tokenizer;
        private readonly GenerationOptions options;
        private readonly Action<string> Log;
        private readonly CancellationToken cancellation;
        private readonly Random random;

        private readonly EventRange timeRange;
        private readonly HashSet<long> startIds;

        private sealed class Context
        {
            public List<Event> Events { get; } = new();
            public List<int> Times { get; } = new();
        }

        public InferencePipeline(
            InferenceSession encoder,
            InferenceSession prefill,
            InferenceSession step,
            Tokenizer tokenizer,
            GenerationOptions options,
            Action<string> Log,
            CancellationToken cancellationToken)
        {
            this.encoder = encoder;
            this.prefill = prefill;
            this.step = step;
            this.tokenizer = tokenizer;
            this.options = options;
            this.Log = Log;
            cancellation = cancellationToken;
            random = options.Seed.HasValue ? new Random(options.Seed.Value) : new Random();

            timeRange = tokenizer.GetRange("t");

            startIds = tokenizer.ContextSos.Values.Select(x => (long)x).Append(Tokenizer.SOS_ID).ToHashSet();
        }

        public GeneratedOsuFile Generate(float[] samples)
        {
            double songLengthMs = samples.Length * 1000.0 / sample_rate;

            int windowCount = samples.Length <= window_samples
                ? 1
                : checked(1 + (int)Math.Ceiling(
                    (samples.Length - window_samples) / (double)stride_samples));

            var hiddenWindows = new List<DenseTensor<float>>(windowCount);
            int[] frameTimes = new int[windowCount];

            Log($"Encoding {windowCount} overlapping audio windows...");

            for (int index = 0; index < windowCount; index++)
            {
                cancellation.ThrowIfCancellationRequested();

                int offset = checked(index * stride_samples);
                float[] window = new float[window_samples];

                int available = Math.Min(
                    window_samples, samples.Length - offset);

                if (available > 0)
                    Array.Copy(samples, offset, window, 0, available);

                // weird but matches python
                frameTimes[index] = (int)(float)(index * stride_samples * 1000.0 / sample_rate);

                hiddenWindows.Add(encode(window));

                Log($"Encoded window {index + 1}/{windowCount}.");
            }

            var timing = new Context();
            var map = new Context();

            generateContext("timing", timing, null, hiddenWindows, frameTimes, songLengthMs);

            generateContext("map", map, timing, hiddenWindows, frameTimes, songLengthMs);

            cancellation.ThrowIfCancellationRequested();

            List<Event> mapEvents = unpackPositions(map.Events);

            Log($"Decoded {timing.Events.Count} timing events and {mapEvents.Count} map events.");

            string content = BeatmapWriter.Write(
                mapEvents, timing.Events, options, Log);

            cancellation.ThrowIfCancellationRequested();

            string fileName = $"beatmap-{Guid.NewGuid():N}.osu";

            var encoding = new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: true);

            byte[] preamble = encoding.GetPreamble();
            byte[] textBytes = encoding.GetBytes(content);

            byte[] fileBytes = new byte[
                checked(preamble.Length + textBytes.Length)];

            Buffer.BlockCopy(
                preamble, 0,
                fileBytes, 0,
                preamble.Length);

            Buffer.BlockCopy(
                textBytes, 0,
                fileBytes, preamble.Length,
                textBytes.Length);

            return new GeneratedOsuFile(
                fileName,
                Path.GetFileName(options.AudioPath),
                fileBytes);
        }

        private DenseTensor<float> encode(float[] window)
        {
            var frames = new DenseTensor<float>(
                window, new[] { 1, window.Length });

            using var outputs = encoder.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor("frames", frames)
            });

            var hidden = outputs
                .First(x => x.Name == "encoder_hidden_states")
                .AsTensor<float>();

            return new DenseTensor<float>(
                hidden.ToArray(), hidden.Dimensions.ToArray());
        }

        private void generateContext(
            string contextType,
            Context current,
            Context? timing,
            IReadOnlyList<DenseTensor<float>> hiddenWindows,
            int[] frameTimes,
            double songLengthMs)
        {
            Log($"Generating {contextType}...");

            for (int index = 0; index < hiddenWindows.Count; index++)
            {
                cancellation.ThrowIfCancellationRequested();

                bool trimLookback = index != 0;
                bool trimLookahead = index != hiddenWindows.Count - 1;

                int frameTime = frameTimes[index];

                List<long> prompt = buildPrompt(
                    contextType, current, timing,
                    frameTime, songLengthMs);

                List<long>? negativePrompt = options.CfgScale > 1
                    ? buildPrompt(
                        contextType, current, timing,
                        frameTime, songLengthMs,
                        negative: true)
                    : null;

                using var cursor = new OnnxDecoderCursor(
                    prefill,
                    step,
                    hiddenWindows[index],
                    prompt,
                    inputVocabularySize: tokenizer.InputVocabularySize,
                    cudaDeviceId: options.CudaDeviceId,
                    outputVocabularySize: tokenizer.OutputVocabularySize);

                using var negativeCursor = negativePrompt is null
                    ? null
                    : new OnnxDecoderCursor(
                        prefill,
                        step,
                        hiddenWindows[index],
                        negativePrompt,
                        inputVocabularySize: tokenizer.InputVocabularySize,
                        cudaDeviceId: options.CudaDeviceId,
                        outputVocabularySize: tokenizer.OutputVocabularySize);

                var history = new List<long>(prompt);
                var generated = new List<long>();

                HashSet<long> stops = getStops(
                    contextType, trimLookback, trimLookahead);

                while (history.Count < maximum_tokens)
                {
                    cancellation.ThrowIfCancellationRequested();

                    float[] logits = cursor.GetLogits();

                    if (negativeCursor is not null)
                    {
                        float[] negativeLogits = negativeCursor.GetLogits();
                        double scale = options.CfgScale;

                        for (int tokenIndex = 0;
                             tokenIndex < logits.Length;
                             tokenIndex++)
                        {
                            double positive = logits[tokenIndex];
                            double negative = negativeLogits[tokenIndex];

                            logits[tokenIndex] = (float)(
                                negative + scale * (positive - negative));
                        }
                    }

                    processLogits(
                        history, logits,
                        trimLookback ? lookback * window_ms : 0);

                    int token = selectToken(logits);

                    generated.Add(token);
                    history.Add(token);

                    if (stops.Contains(token) ||
                        history.Count == maximum_tokens)
                    {
                        break;
                    }

                    cursor.Step(token);
                    negativeCursor?.Step(token);
                }

                int contextEnd = tokenizer.ContextEos[contextType];

                while (generated.Count > 0 &&
                       (generated[^1] == Tokenizer.EOS_ID ||
                        generated[^1] == contextEnd))
                {
                    generated.RemoveAt(generated.Count - 1);
                }

                // removes a terminating timestamp in the excluded region
                if (generated.Count > 0)
                {
                    long last = generated[^1];

                    int lookbackEnd = tokenizer.Encode(
                        new Event("t", (int)(lookback * window_ms / 10)));

                    int lookaheadStart = tokenizer.Encode(
                        new Event(
                            "t", (int)((1 - lookahead) * window_ms / 10)));

                    bool excluded =
                        (trimLookback &&
                         last >= timeRange.StartId &&
                         last < lookbackEnd) ||
                        (trimLookahead &&
                         last >= lookaheadStart &&
                         last < timeRange.EndIdExclusive);

                    if (excluded)
                        generated.RemoveAt(generated.Count - 1);
                }

                current.Events.AddRange(
                    tokenizer.DecodeWindow(generated, frameTime));

                updateEventTimes(
                    current.Events, current.Times,
                    frameTime + window_ms);

                if (trimLookahead)
                {
                    double cutoff =
                        frameTime + (1 - lookahead) * window_ms;

                    for (int i = current.Events.Count - 1; i >= 0; i--)
                    {
                        if (current.Times[i] <= cutoff)
                            break;

                        current.Events.RemoveAt(i);
                        current.Times.RemoveAt(i);
                    }
                }

                Log(
                    $"{contextType}: window {index + 1}/{hiddenWindows.Count}, " +
                    $"{generated.Count} new tokens, " +
                    $"{current.Events.Count} retained events.");
            }

            current.Times.Clear();
            updateEventTimes(current.Events, current.Times, songLengthMs);
        }

        private List<long> buildPrompt(
            string contextType,
            Context current,
            Context? timing,
            int frameTime,
            double songLengthMs,
            bool negative = false)
        {
            var classTokens = buildClassTokens(songLengthMs, negative);

            int lastSv = 100;

            if (contextType == "map")
            {
                for (int i = current.Events.Count - 1; i >= 0; i--)
                {
                    if (current.Events[i].Type == "scroll_speed" &&
                        current.Times[i] < frameTime)
                    {
                        lastSv = current.Events[i].Value;
                        break;
                    }
                }
            }

            classTokens.Add(tokenizer.Encode(
                new Event("scroll_speed", lastSv)));

            double ratio = frameTime / songLengthMs;

            int songPosition = ratio <= 0
                ? -1
                : ratio >= 1
                    ? 101
                    : Math.Clamp((int)Math.Round(
                        ratio * 100, MidpointRounding.ToEven), 0, 100);

            classTokens.Add(tokenizer.Encode(
                new Event("song_position", songPosition)));

            List<long> encodeContext(Context context)
            {
                var tokens = new List<long>();

                for (int i = 0; i < context.Events.Count; i++)
                {
                    if (context.Times[i] >= frameTime &&
                        context.Times[i] < frameTime + window_ms)
                    {
                        tokens.Add(tokenizer.EncodeForWindow(
                            context.Events[i], frameTime));
                    }
                }

                return tokens;
            }

            var ownTokens = encodeContext(current);
            var timingTokens = timing is null
                ? new List<long>()
                : encodeContext(timing);

            int maximumContextTokens = maximum_tokens;

            for (int attempt = 0; attempt <= 10; attempt++)
            {
                var prompt = new List<long>(classTokens)
                {
                    Tokenizer.SOS_ID
                };

                if (contextType == "map")
                {
                    prompt.Add(tokenizer.ContextSos["timing"]);
                    prompt.AddRange(timingTokens.TakeLast(maximumContextTokens));
                    prompt.Add(tokenizer.ContextEos["timing"]);
                }

                prompt.Add(tokenizer.ContextSos[contextType]);
                prompt.AddRange(ownTokens.TakeLast(maximumContextTokens));

                if (prompt.Count < maximum_tokens)
                    return prompt;

                maximumContextTokens /= 2;
            }

            throw new InvalidDataException("Decoder prompt is too long.");
        }

        private List<long> buildClassTokens(
            double songLengthMs,
            bool negative)
        {
            long token(string type, int value) =>
                tokenizer.Encode(new Event(type, value));

            int unknown(string type) =>
                tokenizer.GetRange(type).MaxValue;

            double stars = options.TargetStarRating
                ?? throw new InvalidOperationException(
                    "Provide a target star rating.");

            int difficulty = Math.Clamp(
                (int)(stars * 24 / 12), 0, 23);

            bool hasDescriptors =
                options.Descriptors.Count > 0 ||
                options.NegativeDescriptors.Count > 0;

            int? requestedMapper =
                !negative || hasDescriptors
                    ? options.MapperId
                    : null;

            int mapperIndex = unknown("mapper");

            if (requestedMapper.HasValue &&
                tokenizer.MapperIndices.TryGetValue(
                    requestedMapper.Value, out int knownIndex))
            {
                mapperIndex = knownIndex;
            }

            var result = new List<long>
        {
            token("gamemode", 0),
            token("difficulty", difficulty),
            token("mapper", mapperIndex),
            token("year", options.Year),
            token("hitsounded", options.Hitsounded ? 1 : 0),

            token("song_length",
                Math.Clamp((int)(songLengthMs / 10000), 0, 60)),

            token("global_sv",
                (int)Math.Round(
                    Math.Clamp(options.SliderMultiplier, 0.4, 3.6) * 100,
                    MidpointRounding.ToEven)),

            token("cs",
                Math.Clamp(
                    (int)(options.CircleSize * 20 / 10), 0, 20))
        };

            var descriptors = negative
                ? options.NegativeDescriptors
                : options.Descriptors;

            bool addedDescriptor = false;

            foreach (string name in descriptors.Distinct(StringComparer.Ordinal))
            {
                if (!tokenizer.DescriptorIndices.TryGetValue(
                        name, out int descriptorIndex))
                {
                    throw new ArgumentException(
                        $"Unknown descriptor '{name}'. " +
                        "Choose a descriptor from this model's tokenizer.");
                }

                result.Add(token("descriptor", descriptorIndex));
                addedDescriptor = true;
            }

            if (!addedDescriptor)
                result.Add(token("descriptor", unknown("descriptor")));

            return result;
        }

        private HashSet<long> getStops(string contextType, bool trimLookback, bool trimLookahead)
        {
            var stops = new HashSet<long>
            {
                Tokenizer.EOS_ID,
                tokenizer.ContextEos[contextType]
            };

            if (trimLookback)
            {
                int end = timeRange.StartId +
                    (int)(lookback * window_ms / 10);

                for (int id = timeRange.StartId; id < end; id++)
                    stops.Add(id);
            }

            if (trimLookahead)
            {
                int start = timeRange.EndIdExclusive -
                    (int)(lookahead * window_ms / 10);

                for (int id = start; id < timeRange.EndIdExclusive; id++)
                    stops.Add(id);
            }

            return stops;
        }

        private void processLogits(
            IReadOnlyList<long> history,
            float[] scores,
            double lookbackMs)
        {
            long? latestTime = null;

            for (int i = history.Count - 1; i >= 0; i--)
            {
                long id = history[i];

                if (startIds.Contains(id))
                    break;

                if (id >= timeRange.StartId &&
                    id < timeRange.EndIdExclusive)
                {
                    latestTime = id;
                    break;
                }
            }

            if (latestTime.HasValue)
            {
                for (int id = timeRange.StartId; id < latestTime.Value; id++)
                    scores[id] = float.NegativeInfinity;
            }

            // types_first=false
            for (int id = 0; id < scores.Length; id++)
                scores[id] /= 0.9f;

            if (lookbackMs <= 0)
                return;

            int allowedStart = tokenizer.Encode(
                new Event("t", (int)(lookbackMs / 10)));

            double timeMass = logSumExp(
                scores, timeRange.StartId, timeRange.EndIdExclusive);

            double allowedMass = logSumExp(
                scores, allowedStart, timeRange.EndIdExclusive);

            if (double.IsFinite(allowedMass))
            {
                double shift = timeMass - allowedMass;

                for (int id = allowedStart; id < timeRange.EndIdExclusive; id++)
                    scores[id] = (float)(scores[id] + shift);
            }
            else
            {
                scores[Tokenizer.EOS_ID] = (float)logAdd(
                    scores[Tokenizer.EOS_ID], timeMass);
            }

            for (int id = timeRange.StartId; id < allowedStart; id++)
                scores[id] = float.NegativeInfinity;
        }

        private int selectToken(float[] scores)
        {
            int[] order = Enumerable.Range(0, scores.Length)
                .OrderByDescending(i => scores[i])
                .ToArray();

            if (!float.IsFinite(scores[order[0]]))
                throw new InvalidDataException("All decoder logits are invalid.");

            if (!options.DoSample)
                return order[0];

            double maximum = scores[order[0]];
            double[] weights = new double[order.Length];
            double total = 0;

            for (int i = 0; i < order.Length; i++)
            {
                weights[i] = Math.Exp(scores[order[i]] - maximum);
                total += weights[i];
            }

            double retained = 0;
            int count = 0;

            do
            {
                retained += weights[count++];
            }
            while (count < weights.Length && retained < total * 0.9); // top_p=0.9

            double selection = random.NextDouble() * retained;

            for (int i = 0; i < count; i++)
            {
                selection -= weights[i];

                if (selection <= 0)
                    return order[i];
            }

            return order[count - 1];
        }

        private static double logSumExp(float[] values, int start, int end)
        {
            double maximum = double.NegativeInfinity;

            for (int i = start; i < end; i++)
                maximum = Math.Max(maximum, values[i]);

            if (!double.IsFinite(maximum))
                return maximum;

            double sum = 0;

            for (int i = start; i < end; i++)
                sum += Math.Exp(values[i] - maximum);

            return maximum + Math.Log(sum);
        }

        private static double logAdd(double a, double b)
        {
            if (double.IsNegativeInfinity(a)) return b;
            if (double.IsNegativeInfinity(b)) return a;

            double maximum = Math.Max(a, b);
            return maximum + Math.Log(
                Math.Exp(a - maximum) + Math.Exp(b - maximum));
        }

        private static readonly HashSet<string> non_timed = new() { "bezier_anchor", "perfect_anchor", "catmull_anchor", "red_anchor" };

        private static readonly HashSet<string> timed = new()
        {
            "circle", "spinner", "spinner_end",
            "slider_head", "last_anchor", "slider_end",
            "beat", "measure", "timing_point", "kiai",
            "hold_note", "hold_note_end",
            "drumroll", "drumroll_end",
            "denden", "denden_end", "scroll_speed_change"
        };

        // data_utils.update_event_times
        private static void updateEventTimes(List<Event> events, List<int> times, double endTime)
        {
            int startIndex = times.Count;

            if (startIndex == events.Count)
                return;

            int current = times.Count == 0 ? 0 : times[^1];

            for (int i = startIndex; i < events.Count; i++)
            {
                if (events[i].Type == "t")
                    current = events[i].Value;

                times.Add(current);
            }

            bool interpolate = false;
            double interpolationTime = endTime;

            for (int i = events.Count - 1; i >= startIndex; i--)
            {
                string type = events[i].Type;

                if (timed.Contains(type))
                    interpolate = false;

                if (non_timed.Contains(type))
                    interpolate = true;

                if (!interpolate)
                {
                    interpolationTime = times[i];
                    continue;
                }

                if (!non_timed.Contains(type))
                {
                    times[i] = (int)interpolationTime;
                    continue;
                }

                int j = i;
                int count = 0;
                double otherTime = interpolationTime;

                while (j >= 0)
                {
                    if (events[j].Type == "t")
                    {
                        otherTime = times[j];
                        break;
                    }

                    if (non_timed.Contains(events[j].Type))
                        count++;

                    j--;
                }

                if (j < 0)
                    otherTime = 0;

                interpolationTime = (int)(
                    (interpolationTime - otherTime) /
                    (count + 1) * count + otherTime);

                times[i] = (int)interpolationTime;
            }
        }

        private static List<Event> unpackPositions(
            IReadOnlyList<Event> events)
        {
            var result = new List<Event>();

            for (int i = 0; i < events.Count; i++)
            {
                var item = events[i];

                if (item.Type == "pos")
                {
                    int offsetX = 16;
                    int offsetY = 16;

                    if (i + 1 < events.Count &&
                        events[i + 1].Type == "pos_refine")
                    {
                        int refinement = events[i + 1].Value;
                        offsetX = refinement % 16 * 2;
                        offsetY = refinement / 16 * 2;
                    }

                    result.Add(new Event(
                        "pos_x", (item.Value % 33 - 8) * 32 + offsetX));

                    result.Add(new Event(
                        "pos_y", (item.Value / 33 - 8) * 32 + offsetY));
                }
                else
                    result.Add(item);
            }
            return result;
        }
    }
}
