// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Screens.Select
{
    public partial class BeatmapTitleWedge
    {
        public partial class DifficultyDisplay : CompositeDrawable
        {
            private const float border_weight = 2;

            [Resolved]
            private IBindable<WorkingBeatmap> beatmap { get; set; } = null!;

            [Resolved]
            private IBindable<RulesetInfo> ruleset { get; set; } = null!;

            [Resolved]
            private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

            private ModSettingChangeTracker? settingChangeTracker;

            [Resolved]
            private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

            private DifficultyStatisticsDisplay difficultyStatisticsDisplay = null!;

            private CancellationTokenSource? cancellationSource;

            public DifficultyDisplay()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                Masking = true;
                CornerRadius = 10;
                InternalChildren = new Drawable[]
                {
                    new Container
                    {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding { Bottom = border_weight, Right = border_weight },
                            Child = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Masking = true,
                                CornerRadius = 10 - border_weight,
                                Padding = new MarginPadding { Left = SongSelect.WEDGE_CONTENT_MARGIN, Right = 8f, Vertical = 7.5f },
                                Child = difficultyStatisticsDisplay = new DifficultyStatisticsDisplay(autoSize: false)
                            }
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                // it is not uncommon for the beatmap and the ruleset to change in conjunction during a single update frame.
                // in that process, it is possible for the global bindable triad (beatmap / ruleset / mods) to briefly be partially invalid in combination (e.g. mods invalid for given ruleset).
                // `updateDisplay()` will initiate a difficulty calculation, and if it is allowed to run in that invalid intermediate state, it will loudly fail.
                // therefore, all changes that may initiate a difficulty calculation are debounced until the next frame to ensure the global bindable state is fully consistent -
                // and it's what you'd want to do anyway for performance reasons.
                beatmap.BindValueChanged(_ => Scheduler.AddOnce(updateDisplay));
                ruleset.BindValueChanged(_ => Scheduler.AddOnce(updateDisplay));

                mods.BindValueChanged(m =>
                {
                    settingChangeTracker?.Dispose();

                    updateDifficultyStatistics();

                    if (m.NewValue.Any())
                    {
                        settingChangeTracker = new ModSettingChangeTracker(m.NewValue);
                        settingChangeTracker.SettingChanged += _ => updateDifficultyStatistics();
                    }
                }, true);

                updateDisplay();
            }

            private void updateDisplay()
            {
                cancellationSource?.Cancel();
                cancellationSource = new CancellationTokenSource();
                updateCountStatistics(cancellationSource.Token);
                updateDifficultyStatistics();
            }
            private List<StatisticDifficulty.Data> countStatistics = new List<StatisticDifficulty.Data>();
            private List<StatisticDifficulty.Data> difficultyStatistics = new List<StatisticDifficulty.Data>();

            private void updateCountStatistics(CancellationToken cancellationToken)
            {
                if (beatmap.IsDefault)
                {
                    return;
                }

                Task.Run(() =>
                {
                    // This can take time as it is a synchronous task.
                    // TODO: We're calling `GetPlayableBeatmap` multiple times every map load at song select.
                    var playableBeatmap = beatmap.Value.GetPlayableBeatmap(ruleset.Value);
                    var statistics = playableBeatmap.GetStatistics()
                                                    .Select(s => new StatisticDifficulty.Data(s.Name, s.BarDisplayLength ?? 0, s.BarDisplayLength ?? 0, 1, s.Content))
                                                    .ToList();

                    Schedule(() =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;
                        countStatistics = statistics;
                    });
                }, cancellationToken);
            }

            private void updateDifficultyStatistics() => Scheduler.AddOnce(() =>
            {
                if (beatmap.IsDefault || ruleset.Value == null)
                {
                    difficultyStatisticsDisplay.Statistics = Array.Empty<StatisticDifficulty.Data>();
                    return;
                }

                Ruleset rulesetInstance = ruleset.Value.CreateInstance();

                var displayAttributes = rulesetInstance.GetBeatmapAttributesForDisplay(beatmap.Value.BeatmapInfo, mods.Value).ToList();
                difficultyStatistics = displayAttributes.Select(a => new StatisticDifficulty.Data(a)).ToList();
                difficultyStatisticsDisplay.Statistics = difficultyStatistics.Concat(countStatistics).ToList();
            });
        }
    }
}
