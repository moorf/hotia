// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using NAudio.Wave;

namespace hotia.AiMapper
{
    public static class AudioLoader
    {
        public static AudioData LoadMono16Khz(string path)
        {
            Console.WriteLine("loading");
            using var reader = new AudioFileReader(path);
            int channels = reader.WaveFormat.Channels;
            int sourceRate = reader.WaveFormat.SampleRate;

            var interleaved = new List<float>((int)Math.Min(reader.Length / sizeof(float), int.MaxValue));

            float[] buffer = new float[8192];

            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                for (int i = 0; i < read; i++)
                    interleaved.Add(buffer[i]);

            if (channels < 1)
                throw new InvalidDataException("Audio file has an invalid channel count.");

            int frameCount = interleaved.Count / channels;
            float[] mono = new float[frameCount];

            for (int frame = 0; frame < frameCount; frame++)
            {
                double sum = 0;
                for (int channel = 0; channel < channels; channel++)
                    sum += interleaved[frame * channels + channel];

                mono[frame] = (float)(sum / channels);
            }

            const int targetrate = 16000;
            float[] resampled = sourceRate == targetrate ? mono : linearResample(mono, sourceRate, targetrate);

            double peak = resampled.Length == 0 ? 0 : resampled.Max(x => Math.Abs((double)x));
            if (peak > 0.0)
                for (int i = 0; i < resampled.Length; i++)
                    resampled[i] /= (float)peak;

            return new AudioData(resampled, targetrate, TimeSpan.FromSeconds((double)resampled.Length / targetrate));
        }

        private static float[] linearResample(float[] input, int sourceRate, int targetRate) //hotiaTODO: consider using NAudio resampler
        {
            if (input.Length == 0) return Array.Empty<float>();

            int outputLength = Math.Max(1, (int)Math.Round(input.Length * (double)targetRate / sourceRate));
            float[] output = new float[outputLength];

            for (int i = 0; i < outputLength; i++)
            {
                double sourcePosition = i * (double)sourceRate / targetRate;
                int left = Math.Min((int)sourcePosition, input.Length - 1);
                int right = Math.Min(left + 1, input.Length - 1);
                double fraction = sourcePosition - left;
                output[i] = (float)(input[left] * (1.0 - fraction) + input[right] * fraction);
            }
            return output;
        }

        public sealed record AudioData(float[] Samples, int SampleRate, TimeSpan Duration);
    }
}
