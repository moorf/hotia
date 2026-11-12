// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace hotia.AiMapper.Inference
{
    public static class OnnxInferenceDiagnostics
    {
        public static DenseTensor<float> EncodeWindow(
            InferenceSession encoder,
            float[] samples)
        {
            ArgumentNullException.ThrowIfNull(samples);

            if (samples.Length == 0)
                throw new ArgumentException("Audio window is empty.", nameof(samples));

            var frames = new DenseTensor<float>(
                samples,
                new[] { 1, samples.Length });

            using var outputs = encoder.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor("frames", frames)
            });

            var hidden = outputs
                .First(x => x.Name == "encoder_hidden_states")
                .AsTensor<float>();

            // Own this managed copy after the encoder output collection is disposed.
            return new DenseTensor<float>(
                hidden.ToArray(),
                hidden.Dimensions.ToArray());
        }

        public static void VerifyCachedDecoder(
            InferenceSession decoder,
            InferenceSession decoderPrefill,
            InferenceSession decoderStep,
            Tokenizer tokenizer,
            Tensor<float> encoderHiddenStates,
            Action<string>? log = null,
            int steps = 6,
            double tolerance = 0.005,
            CancellationToken cancellationToken = default,
            int cudaDeviceId = 0)
        {
            // Numerical test prefix only; not a complete production prompt.
            long[] seedPrompt =
            {
                tokenizer.Encode(new Event("gamemode", 0)),
                Tokenizer.SOS_ID,
                tokenizer.ContextSos["timing"]
            };

            var prefix = seedPrompt.ToList();

            using var cursor = new OnnxDecoderCursor(
                decoderPrefill,
                decoderStep,
                encoderHiddenStates,
                prefix,
                inputVocabularySize: tokenizer.InputVocabularySize,
                cudaDeviceId: cudaDeviceId,
                outputVocabularySize: tokenizer.OutputVocabularySize);

            for (int index = 0; index <= steps; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                float[] cached = cursor.GetLogits();
                float[] full = RunFullDecoder(
                    decoder, encoderHiddenStates, prefix);

                double maximumDifference = 0;

                for (int token = 0; token < cached.Length; token++)
                {
                    if (!float.IsFinite(cached[token]) ||
                        !float.IsFinite(full[token]))
                    {
                        throw new InvalidDataException(
                            "Non-finite decoder logits encountered.");
                    }

                    maximumDifference = Math.Max(
                        maximumDifference,
                        Math.Abs((double)cached[token] - full[token]));
                }

                int cachedBest = ArgMax(cached);
                int fullBest = ArgMax(full);

                log?.Invoke(
                    $"Cache parity: prefix={prefix.Count}, " +
                    $"max error={maximumDifference:E3}, " +
                    $"argmax cached/full={cachedBest}/{fullBest}");

                if (maximumDifference > tolerance || cachedBest != fullBest)
                {
                    throw new InvalidDataException(
                        "Cached decoder parity failed. " +
                        "Do not use cached generation until this is resolved.");
                }

                if (index == steps)
                    break;

                // Continue even if this is EOS: this is a tensor/cache test,
                // not a production generation loop.
                prefix.Add(cachedBest);
                cursor.Step(cachedBest);
            }
        }

        private static float[] RunFullDecoder(
            InferenceSession decoder,
            Tensor<float> encoderHiddenStates,
            IReadOnlyList<long> prefix)
        {
            var ids = new DenseTensor<long>(
                prefix.ToArray(),
                new[] { 1, prefix.Count });

            using var outputs = decoder.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor("decoder_input_ids", ids),
                NamedOnnxValue.CreateFromTensor(
                    "encoder_hidden_states", encoderHiddenStates)
            });

            var logits = outputs
                .First(x => x.Name == "logits")
                .AsTensor<float>();

            if (logits.Rank != 3 ||
                logits.Dimensions[0] != 1 ||
                logits.Dimensions[1] != prefix.Count ||
                logits.Dimensions[2] != 4097)
            {
                throw new InvalidDataException(
                    "Unexpected full decoder logits shape.");
            }

            var last = new float[4097];

            for (int token = 0; token < last.Length; token++)
                last[token] = logits[0, prefix.Count - 1, token];

            return last;
        }

        private static int ArgMax(float[] values)
        {
            int best = 0;

            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > values[best])
                    best = i;
            }

            return best;
        }
    }
}
