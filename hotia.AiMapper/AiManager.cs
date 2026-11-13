// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Screens;
using osu.Game.Ai;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Utils;

namespace hotia.AiMapper
{
    public partial class AiManager : Drawable, IAiManager
    {
        public Action<Notification>? PostNotification { get; set; }

        /// <summary>
        /// Receives the generated .osz path.
        /// </summary>
        public Action<string>? Generated { get; set; }

        /// <summary>
        /// Saves path for needed for generation
        /// </summary>
        public Action<string>? PathSaved { get; set; }

        /// <summary>
        /// If null: uses the generator's automatic model cache/download.
        /// </summary>
        public string? ModelDirectory { get; set; }

        public bool Archive { get; set; } = true;

        private readonly SemaphoreSlim importLock = new(1, 1); // ensuring only one map generating at the time hotiaTODO: test more

        public IEnumerable<string> HandledExtensions { get; } = SupportedExtensions.AUDIO_EXTENSIONS;

        private string currentPath { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]

        public async Task Import(params string[] paths)
        {
            ArgumentNullException.ThrowIfNull(paths);

            if (!Archive)
                throw new InvalidOperationException("Archive must be enabled for now.");

            if (paths.Length < 0)
                throw new InvalidOperationException("No audio path given for generation.");
            currentPath = paths.First();

            PathSaved?.Invoke(currentPath);
        }

        public async Task Generate(GenerationOptions generationOptions)
        {
            await importLock.WaitAsync();
            try
            {
                var notification = new ProgressNotification
                {
                    Text = "Preparing beatmap generation..."
                };

                PostNotification?.Invoke(notification);

                string archivePath = await BeatmapGenerator.Generate(
                    progressNotification: notification,
                    generationOptions)
                    ?? throw new InvalidOperationException("Generation finished without producing an archive.");

                Generated?.Invoke(archivePath);
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
