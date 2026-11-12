// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using osu.Game.Database;
using osu.Game.Overlays.Notifications;
using osu.Game.Ai;
using osu.Game.Utils;

namespace hotia.AiMapper
{
    public sealed class AiManager : IAiManager
    {
        public Action<Notification>? PostNotification { get; set; }

        /// <summary>
        /// Receives the generated .osz path.
        /// </summary>
        public Action<string>? Generated { get; set; }

        /// <summary>
        /// If null: uses the generator's automatic model cache/download.
        /// </summary>
        public string? ModelDirectory { get; set; }

        public bool Archive { get; set; } = true;

        private readonly SemaphoreSlim importLock = new(1, 1); // ensuring only one map generating at the time hotiaTODO: test more

        public IEnumerable<string> HandledExtensions { get; } = SupportedExtensions.AUDIO_EXTENSIONS;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
        public async Task Import(params string[] paths)
        {
            ArgumentNullException.ThrowIfNull(paths);

            if (!Archive)
                throw new InvalidOperationException("Archive must be enabled for now.");

            await importLock.WaitAsync();

            try
            {
                foreach (string path in paths)
                {
                    var notification = new ProgressNotification
                    {
                        Text = "Preparing beatmap generation..."
                    };

                    PostNotification?.Invoke(notification);

                    string archivePath = await BeatmapGenerator.Generate(
                        audioPath: path,
                        progressNotification: notification,
                        modelDirectory: ModelDirectory,
                        archive: true)
                        ?? throw new InvalidOperationException("Generation finished without producing an archive.");

                    Generated?.Invoke(archivePath);
                }
            }
            finally
            {
                importLock.Release();
            }
        }

        public Task Import(ImportTask[] tasks, ImportParameters parameters = default)
        {
            ArgumentNullException.ThrowIfNull(tasks);
            return Import(tasks.Select(task => task.Path).ToArray());
        }
    }
}
