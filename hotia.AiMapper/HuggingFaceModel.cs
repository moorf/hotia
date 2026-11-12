// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.Text.Json;
using osu.Game.Overlays.Notifications;

namespace hotia.AiMapper
{
    public static class HuggingFaceModel
    {
        private const string repository = "moorf/Mapperatorinator-ONNX";

        private const string apiurl = "https://huggingface.co/api/models/" + repository + "/tree/main?recursive=true&expand=true";

        private const string downloadurl = "https://huggingface.co/" + repository + "/resolve/main/";

        private static readonly HttpClient http = new()
        {
            Timeout = TimeSpan.FromMinutes(30)
        };

        public static string DefaultDirectory =>
            Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mapperatorinator", "Models");

        public static bool IsDownloaded(string directory)
        {
            if (!Directory.Exists(directory))
                return false;

            foreach (string name in new[] { "encoder.onnx", "decoder_prefill.onnx", "decoder_step.onnx", "tokenizer.json" })
            {
                string path = Path.Combine(directory, name);

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    return false;
            }
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
        public static async Task<string> DownloadAsync(ProgressNotification progressNotification, CancellationToken cancellationToken = default)
        {
            string root = DefaultDirectory;

            Directory.CreateDirectory(root);

            progressNotification.Text = "Checking :hugging_face: for model files...";

            using var response = await http.GetAsync(
                apiurl,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var files = json.RootElement
                .EnumerateArray()
                .Where(x =>
                    x.TryGetProperty("type", out var type) &&
                    type.GetString() == "file" &&
                    x.TryGetProperty("path", out _))
                .Select(x => new
                {
                    Path = x.GetProperty("path").GetString()!,
                    Size = x.TryGetProperty("size", out var size)
                        ? size.GetInt64()
                        : -1
                })
                .Where(x =>
                    !x.Path.Equals("README.md",
                        StringComparison.OrdinalIgnoreCase) &&
                    !x.Path.Equals(".gitattributes",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (files.Count == 0)
                throw new Exception("No model files in the Hugging Face repository.");

            int completed = 0;

            foreach (var file in files)
            {
                string destination = Path.GetFullPath(
                    Path.Combine(root, file.Path.Replace('/', Path.DirectorySeparatorChar)));

                string prefix = Path.GetFullPath(root) + Path.DirectorySeparatorChar;

                if (!destination.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Invalid model file path: " + file.Path);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                if (File.Exists(destination) &&
                   (file.Size < 0 || new FileInfo(destination).Length == file.Size))
                {
                    completed++;
                    progressNotification.Text = $"Already downloaded: {file.Path} " + $"({completed}/{files.Count})";
                    continue;
                }

                progressNotification.Text = $"Downloading: {file.Path} " + $"({completed + 1}/{files.Count})";

                string url = downloadurl +
                    string.Join("/", file.Path.Split('/').Select(Uri.EscapeDataString));

                string temporary = destination + ".download";
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var download = await http.GetAsync(
                        url,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    download.EnsureSuccessStatusCode();

                    await using var input =
                        await download.Content.ReadAsStreamAsync(cancellationToken);

                    await using (var output = new FileStream(
                        temporary,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        81920,
                        useAsync: true))
                    {
                        await input.CopyToAsync(output, cancellationToken);
                    }

                    if (file.Size >= 0 &&
                        new FileInfo(temporary).Length != file.Size)
                    {
                        throw new IOException(
                            $"Downloaded file has an incorrect size: {file.Path}");
                    }

                    File.Move(temporary, destination, true);
                }
                finally
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }

                completed++;
            }

            if (!IsDownloaded(root))
            {
                throw new Exception("Download finished, but downloaded files are not found.");
            }

            progressNotification.Text = "All model files are ready.";

            return root;
        }
    }
}
