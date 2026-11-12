// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace hotia.AiMapper.Inference
{
    public sealed class OnnxDecoderCursor : IDisposable
    {
        private readonly InferenceSession stepSession;
        private readonly int layerCount;
        private readonly int outputVocabularySize;
        private readonly int cudaDeviceId;
        private readonly BoundOutputs prefillOutputs;
        private BoundOutputs? stepOutputs;
        private bool disposed;
        public int TokenCount { get; private set; }

        public OnnxDecoderCursor(
            InferenceSession prefillSession,
            InferenceSession stepSession,
            Tensor<float> encoderHiddenStates,
            IReadOnlyList<long> prompt,
            int inputVocabularySize = 12505,
            int layerCount = 12,
            int cudaDeviceId = 0,
            int outputVocabularySize = 4097)
        {
            ArgumentNullException.ThrowIfNull(prefillSession);
            ArgumentNullException.ThrowIfNull(stepSession);
            ArgumentNullException.ThrowIfNull(encoderHiddenStates);
            ArgumentNullException.ThrowIfNull(prompt);

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(layerCount);
            ArgumentOutOfRangeException.ThrowIfNegative(cudaDeviceId);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outputVocabularySize);

            if (encoderHiddenStates.Rank != 3 || encoderHiddenStates.Dimensions[0] != 1)
                throw new ArgumentException("Expected batch-one encoder states shaped [1, T, D].", nameof(encoderHiddenStates));

            if (prompt.Count == 0)
                throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));

            foreach (long id in prompt)
            {
                if (id == Tokenizer.PAD_ID)
                    throw new NotSupportedException("This cursor accepts unpadded prompts only.");

                if (id < 0 || id >= inputVocabularySize)
                    throw new ArgumentOutOfRangeException(nameof(prompt), $"Invalid input token ID: {id}.");
            }

            this.stepSession = stepSession;
            this.layerCount = layerCount;
            this.outputVocabularySize = outputVocabularySize;
            this.cudaDeviceId = cudaDeviceId;

            long[] promptData = prompt.ToArray();

            using var ids = OrtValue.CreateTensorValueFromMemory(promptData, new long[] { 1, promptData.Length });

            float[] hiddenData = encoderHiddenStates.ToArray();

            long[] hiddenShape = encoderHiddenStates.Dimensions.ToArray().Select(d => (long)d).ToArray();

            using var hidden = OrtValue.CreateTensorValueFromMemory(hiddenData, hiddenShape);

            using var binding = prefillSession.CreateIoBinding();

            binding.BindInput("decoder_input_ids", ids);
            binding.BindInput("encoder_hidden_states", hidden);

            bindOutputs(binding, includeCross: true);

            BoundOutputs outputs = runBound(prefillSession, binding);

            try
            {
                validateOutputs(outputs, expectedSelfLength: prompt.Count, includeCross: true);
                prefillOutputs = outputs;
                TokenCount = prompt.Count;
            }
            catch
            {
                outputs.Dispose();
                throw;
            }
        }

        public float[] GetLogits()
        {
            throwIfDisposed();
            BoundOutputs outputs = stepOutputs ?? prefillOutputs;
            return outputs["logits"].GetTensorDataAsSpan<float>().ToArray();
        }

        public void Step(long tokenId)
        {
            throwIfDisposed();

            if (tokenId < 0 || tokenId >= outputVocabularySize)
                throw new ArgumentOutOfRangeException(nameof(tokenId), "Step expects a token from the output vocabulary.");

            int nextLength = checked(TokenCount + 1);

            BoundOutputs selfOutputs = stepOutputs ?? prefillOutputs;

            using var ids = OrtValue.CreateTensorValueFromMemory(new long[] { tokenId }, new long[] { 1, 1 });

            using var binding = stepSession.CreateIoBinding();

            binding.BindInput("decoder_input_ids", ids);

            for (int layer = 0; layer < layerCount; layer++)
            {
                binding.BindInput($"past_self_k_{layer}", selfOutputs[$"present_self_k_{layer}"]);

                binding.BindInput($"past_self_v_{layer}", selfOutputs[$"present_self_v_{layer}"]);

                binding.BindInput($"cross_k_{layer}", prefillOutputs[$"present_cross_k_{layer}"]);

                binding.BindInput($"cross_v_{layer}", prefillOutputs[$"present_cross_v_{layer}"]);
            }

            bindOutputs(binding, includeCross: false);

            BoundOutputs nextOutputs = runBound(stepSession, binding);
            try
            {
                validateOutputs(nextOutputs, expectedSelfLength: nextLength, includeCross: false);
            }
            catch
            {
                nextOutputs.Dispose();
                throw;
            }

            BoundOutputs? previousStepOutputs = stepOutputs;

            stepOutputs = nextOutputs;
            TokenCount = nextLength;

            previousStepOutputs?.Dispose();
        }

        private void bindOutputs(OrtIoBinding binding, bool includeCross)
        {
            using var cpuMemory = new OrtMemoryInfo("Cpu", OrtAllocatorType.ArenaAllocator, 0, OrtMemType.Default);

            using var cudaMemory = new OrtMemoryInfo("Cuda", OrtAllocatorType.DeviceAllocator, cudaDeviceId, OrtMemType.Default);

            binding.BindOutputToDevice("logits", cpuMemory);

            for (int layer = 0; layer < layerCount; layer++)
            {
                // ONNX Runtime allocates these dynamically on CUDA
                binding.BindOutputToDevice($"present_self_k_{layer}", cudaMemory);

                binding.BindOutputToDevice($"present_self_v_{layer}", cudaMemory);

                if (includeCross)
                {
                    binding.BindOutputToDevice($"present_cross_k_{layer}", cudaMemory);

                    binding.BindOutputToDevice($"present_cross_v_{layer}", cudaMemory);
                }
            }
        }

        private static BoundOutputs runBound(InferenceSession session, OrtIoBinding binding)
        {
            using var runOptions = new RunOptions();

            binding.SynchronizeBoundInputs();

            session.RunWithBinding(runOptions, binding);

            binding.SynchronizeBoundOutputs();

            string[] names = binding.GetOutputNames().ToArray();
            var values = binding.GetOutputValues();

            try
            {
                return new BoundOutputs(names, values);
            }
            catch
            {
                values.Dispose();
                throw;
            }
        }

        private void validateOutputs(BoundOutputs outputs, int expectedSelfLength, bool includeCross)
        {
            var info = outputs["logits"].GetTensorTypeAndShape();
            long[] shape = info.Shape;

            if (shape.Length != 2 || shape[0] != 1 || shape[1] != outputVocabularySize)
            {
                throw new InvalidDataException(
                    $"Decoder logits must have shape[1, {outputVocabularySize}], got {string.Join(", ", shape)}]");
            }

            for (int layer = 0; layer < layerCount; layer++)
            {
                validateCache(outputs[$"present_self_k_{layer}"], expectedSelfLength);
                validateCache(outputs[$"present_self_v_{layer}"], expectedSelfLength);

                if (includeCross)
                {
                    validateCache(outputs[$"present_cross_k_{layer}"], expectedLength: null);
                    validateCache(outputs[$"present_cross_v_{layer}"], expectedLength: null);
                }
            }
        }

        private static void validateCache(
            OrtValue value,
            int? expectedLength)
        {
            var info = value.GetTensorTypeAndShape();
            long[] shape = info.Shape;

            if (shape.Length != 4 ||
                shape[0] != 1 ||
                shape[1] != 12 ||
                shape[3] != 64 ||
                (expectedLength.HasValue &&
                 shape[2] != expectedLength.Value))
            {
                throw new InvalidDataException($"Unexpected decoder cache shape: [{string.Join(", ", shape)}].");
            }
        }

        private void throwIfDisposed()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            stepOutputs?.Dispose();
            prefillOutputs.Dispose();
        }

        private sealed class BoundOutputs : IDisposable
        {
            private readonly IDisposableReadOnlyCollection<OrtValue> owner;
            private readonly Dictionary<string, OrtValue> byName;
            private bool disposed;

            public BoundOutputs(
                IReadOnlyList<string> names,
                IDisposableReadOnlyCollection<OrtValue> owner)
            {
                if (names.Count != owner.Count)
                {
                    throw new InvalidDataException(
                        "ONNX output names and values have different counts.");
                }

                this.owner = owner;

                byName = new Dictionary<string, OrtValue>(
                    names.Count,
                    StringComparer.Ordinal);

                int index = 0;

                foreach (OrtValue value in owner)
                    byName.Add(names[index++], value);
            }

            public OrtValue this[string name]
            {
                get
                {
                    ObjectDisposedException.ThrowIf(disposed, this);

                    if (byName.TryGetValue(name, out OrtValue? value))
                        return value;

                    throw new InvalidDataException($"Missing ONNX output '{name}'.");
                }
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;

                owner.Dispose();
                byName.Clear();
            }
        }
    }
}
