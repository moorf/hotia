// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.IO.Compression;
using Microsoft.ML.OnnxRuntime;
using osu.Framework.Extensions;
using osu.Game.Ai;
using osu.Game.Overlays.Notifications;

namespace hotia.AiMapper
{
    public sealed class GeneratedOsuFile
    {
        public string FileName { get; }

        public string AudioFileName { get; }

        public ReadOnlyMemory<byte> Content { get; }

        public string? ArchivePath { get; }

        internal GeneratedOsuFile(
            string fileName,
            string audioFileName,
            byte[] content,
            string? archivePath = null)
        {
            FileName = fileName;
            AudioFileName = audioFileName;
            Content = content;
            ArchivePath = archivePath;
        }
    }

    public static class BeatmapGenerator
    {
        public static async Task<string> Generate(
        ProgressNotification progressNotification,
        GenerationOptions options,
        CancellationToken cancellationToken = default)
        {

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(options.AudioPath))
                    throw new ArgumentException("Audio path was not provided.", nameof(options.AudioPath));

                options.AudioPath = Path.GetFullPath(options.AudioPath);

                if (!File.Exists(options.AudioPath))
                    throw new FileNotFoundException("The audio file was not found.", options.AudioPath);

                string resolvedModels;

                if (string.IsNullOrWhiteSpace(options.ModelDirectory))
                {
                    resolvedModels = HuggingFaceModel.DefaultDirectory;

                    if (!HuggingFaceModel.IsDownloaded(resolvedModels))
                    {
                        progressNotification.Text = "Downloading model files...";

                        resolvedModels =
                            await HuggingFaceModel.DownloadAsync(progressNotification, cancellationToken).ConfigureAwait(true);
                    }
                    else
                        progressNotification.Text = "Using cached model files.";
                }
                else
                {
                    resolvedModels = Path.GetFullPath(options.ModelDirectory);
                    progressNotification.Text = ("Using model files: " + resolvedModels);
                }
                options.ModelDirectory = resolvedModels;
                cancellationToken.ThrowIfCancellationRequested();

                if (options.Title.Length == 0) options.Title = DateTime.Now.ToString();
                if (options.Artist.Length == 0) options.Artist = Guid.NewGuid().ToString().ComputeSHA2Hash().Substring(0, 15);

                GeneratedOsuFile result = await GenerateAsync(options, progressNotification, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (true) //archive
                {
                    progressNotification.Text = "Packaging beatmap and audio...";
                    progressNotification.Progress = 0.97f;

                    string archivePath = await Task.Run(() =>
                    writeArchiveAsync(result, options.AudioPath, cancellationToken),
                        cancellationToken).ConfigureAwait(true);

                    result = new GeneratedOsuFile(
                        result.FileName,
                        result.AudioFileName,
                        result.Content.ToArray(),
                        archivePath);

                    progressNotification.Text = "Created archive: " + archivePath;
                    progressNotification.Text = $"Generated {result.FileName} and saved its archive.";
                    progressNotification.State = ProgressNotificationState.Completed;
                    return result.ArchivePath ?? result.FileName;
                }

                progressNotification.Text = $"Generated {result.FileName}";
                progressNotification.State = ProgressNotificationState.Completed;

                return result.FileName;
            }
            catch (OperationCanceledException)
            {
                progressNotification.Text = ("Beatmap generation cancelled.");
                progressNotification.State = ProgressNotificationState.Cancelled;
                throw;
            }
            catch (Exception ex)
            {
                progressNotification.Text = ("Beatmap generation failed: " + ex.Message);
                progressNotification.State = ProgressNotificationState.Cancelled;
                throw;
            }
        }

        private static List<string> parseDescriptors(
            string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new();

            var result = new List<string>();
            var seen = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (string value in text.Split(
                         new[] { ';', '\r', '\n' },
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (seen.Add(value))
                    result.Add(value);
            }

            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
        private static async Task<string> writeArchiveAsync(
            GeneratedOsuFile beatmap,
            string audioPath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string archivePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                Path.GetFileNameWithoutExtension(beatmap.FileName) + ".osz");

            bool created = false;

            try
            {
                await using (var output =
                    new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    created = true;

                    using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
                    {
                        ZipArchiveEntry mapEntry = zip.CreateEntry(beatmap.FileName, CompressionLevel.Optimal);

                        await using (Stream mapStream = mapEntry.Open())
                        {
                            await mapStream.WriteAsync(beatmap.Content, cancellationToken);
                        }

                        ZipArchiveEntry audioEntry = zip.CreateEntry(beatmap.AudioFileName, CompressionLevel.Optimal);

                        await using (Stream audioStream = audioEntry.Open())
                        await using (var input =
                            new FileStream(audioPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
                        {
                            await input.CopyToAsync(audioStream, cancellationToken);
                        }
                    }

                    await output.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                return archivePath;
            }
            catch
            {
                if (created)
                {
                    File.Delete(archivePath);
                }
                throw;
            }
        }
        public static Task<GeneratedOsuFile> GenerateAsync(GenerationOptions options, ProgressNotification progressNotification, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(options.AudioPath))
                    throw new FileNotFoundException("Select a valid audio file.", options.AudioPath);

                if (options.GameMode != "osu!standard" && options.GameMode != "0")
                    throw new NotSupportedException("hotiaTODO: test for non osu!std");

                if (options.TargetStarRating is not double stars || !double.IsFinite(stars) || stars < 0 || stars > 12)
                    throw new ArgumentException("Star rating must be between 0 and 12.");

                if (options.Year < 2007 || options.Year > 2024)
                    throw new ArgumentException("Year must be between 2007 and 2024.");

                if (!double.IsFinite(options.CfgScale) || options.CfgScale < 1 || options.CfgScale > 10)
                    throw new ArgumentException("CFG scale must be between 1 and 10.");

                if (options.CfgScale == 1 && options.NegativeDescriptors.Count > 0)
                    options.CfgScale += 1;
                //throw new ArgumentException("Negative descriptors require CFG scale greater than 1.");

                if (options.MapperId is <= 0)
                    throw new ArgumentException("Mapper ID must be a positive osu! user ID.");

                string directory = options.ModelDirectory;

                foreach (string name in new[] { "encoder.onnx", "decoder_prefill.onnx", "decoder_step.onnx", "tokenizer.json" })
                {
                    string path = Path.Combine(directory, name);

                    if (!File.Exists(path))
                        throw new FileNotFoundException($"Missing required file: {name}", path);
                }

                void Log(string message) => progressNotification.Text = message;

                Log("Decoding and peak-normalizing audio...");
                var audio = AudioLoader.LoadMono16Khz(options.AudioPath);

                if (audio.Samples.Length == 0)
                    throw new InvalidDataException("The audio contains no samples.");

                var tokenizer = Inference.Tokenizer.Load(
                    Path.Combine(directory, "tokenizer.json"));

                if (tokenizer.OutputVocabularySize != 4097 || tokenizer.InputVocabularySize != 12505)
                    throw new InvalidDataException("This engine expects the supplied V32 tokenizer.");

                foreach (string descriptor in options.Descriptors.Concat(options.NegativeDescriptors))
                {
                    if (!tokenizer.DescriptorIndices.ContainsKey(descriptor))
                        throw new ArgumentException($"Descriptor '{descriptor}' is not in this model's vocabulary.");
                }

                if (options.MapperId.HasValue && !tokenizer.MapperIndices.ContainsKey(options.MapperId.Value))
                {
                    Log($"Mapper ID {options.MapperId.Value} is not known to this checkpoint. " + "Using unknown mapper conditioning.");
                }

                Log(
                    $"Configuration: {stars:0.##} ★, year={options.Year}, " +
                    $"mapper={options.MapperId?.ToString() ?? "unknown"}, " +
                    $"CFG={options.CfgScale:0.##}");

                if (options.Descriptors.Count > 0)
                    Log("Descriptors: " + string.Join("; ", options.Descriptors));

                if (options.NegativeDescriptors.Count > 0)
                    Log("Negative descriptors: " + string.Join("; ", options.NegativeDescriptors));

                Log($"Initializing ONNX. CudaDeviceId: {options.CudaDeviceId}...");

                (SessionOptions s_options, string deviceName) = CreateOnnxOptions(options.CudaDeviceId);

                using var sessionOptions = s_options;

                Log($"ONNX Device: {deviceName}");

                Log("Loading encoder...");
                using var encoder = new InferenceSession(Path.Combine(directory, "encoder.onnx"), sessionOptions);

                Log("Loading decoder prefill...");
                using var prefill = new InferenceSession(Path.Combine(directory, "decoder_prefill.onnx"), sessionOptions);

                Log("Loading decoder step...");
                using var step = new InferenceSession(Path.Combine(directory, "decoder_step.onnx"), sessionOptions);

                if (options.VerifyDecoderCache)
                {
                    Log("Verifying decoder cache...");
                    cancellationToken.ThrowIfCancellationRequested();

                    string decoderPath = Path.Combine(directory, "decoder.onnx");

                    if (!File.Exists(decoderPath))
                        throw new FileNotFoundException($"decoder.onnx not found in {decoderPath}");

                    float[] verificationWindow = new float[262016];

                    Array.Copy(
                        audio.Samples,
                        verificationWindow,
                        Math.Min(
                            audio.Samples.Length,
                            verificationWindow.Length));

                    var hidden = Inference.OnnxInferenceDiagnostics.EncodeWindow(encoder, verificationWindow);
                    
                    Log("Loading full decoder for parity verification...");

                    using (var fullDecoder = new InferenceSession(decoderPath, sessionOptions))
                    {
                        Inference.OnnxInferenceDiagnostics.VerifyCachedDecoder(
                            decoder: fullDecoder,
                            decoderPrefill: prefill,
                            decoderStep: step,
                            tokenizer: tokenizer,
                            encoderHiddenStates: hidden,
                            log: Log,
                            steps: 6,
                            tolerance: 0.005,
                            cancellationToken: cancellationToken,
                            cudaDeviceId: options.CudaDeviceId);
                    }

                    Log("Parity verification passed!");
                }

                var pipeline = new Inference.InferencePipeline(
                    encoder,
                    prefill,
                    step,
                    tokenizer,
                    options,
                    Log,
                    cancellationToken);

                Log("Starting to generate...");

                GeneratedOsuFile result = pipeline.Generate(audio.Samples);

                cancellationToken.ThrowIfCancellationRequested();

                Log($"Generated beatmap: {result.FileName}");

                return result;

            }, cancellationToken);
        }

        public static (SessionOptions, string) CreateOnnxOptions(int deviceId = 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(deviceId);

            var options = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                EnableCpuMemArena = true,
                EnableMemoryPattern = false
            };

            try
            {
                // GPU
                options.AppendExecutionProvider_CUDA(deviceId);

                return (options, "GPU");
            }
            catch (Exception)
            {
                // CPU
                options.Dispose();

                return (new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                    EnableCpuMemArena = true,
                    EnableMemoryPattern = false
                }, "CPU");
            }
        }
    }
}
