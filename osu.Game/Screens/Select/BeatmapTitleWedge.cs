// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.Chat;
using osu.Game.Overlays;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;
using osuTK;

namespace osu.Game.Screens.Select
{
    public partial class BeatmapTitleWedge : VisibilityContainer
    {
        private const float corner_radius = 10;

        [Resolved]
        private IBindable<WorkingBeatmap> working { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

        [Resolved]
        private IBindable<SongSelect.BeatmapSetLookupResult?> onlineLookupResult { get; set; } = null!;

        public float TopPadding { get; init; }

        protected override bool StartHidden => true;

        private ModSettingChangeTracker? settingChangeTracker;

        private BeatmapSetOnlineStatusPill statusPill = null!;
        private OsuHoverContainer titleLink = null!;
        private MarqueeContainer titleLabel = null!;
        private OsuHoverContainer artistLink = null!;
        private MarqueeContainer artistLabel = null!;
        private FillFlowContainer nameLine = null!;
        private OsuSpriteText difficultyText = null!;
        private OsuSpriteText mappedByText = null!;
        private OsuHoverContainer mapperLink = null!;
        private OsuSpriteText mapperText = null!;
        internal string DisplayedTitle { get; private set; } = string.Empty;
        internal string DisplayedArtist { get; private set; } = string.Empty;


        private StarRatingDisplayH starRatingDisplay = null!;
        private StatisticPlayCount playCount = null!;
        private FavouriteButton favouriteButton = null!;
        private Statistic lengthStatistic = null!;
        private Statistic bpmStatistic = null!;

        [Resolved]
        private ISongSelect? songSelect { get; set; }

        [Resolved]
        private LocalisationManager localisation { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

        private FillFlowContainer statisticsFlow = null!;

        public BeatmapTitleWedge()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [Resolved]
        private ILinkHandler? linkHandler { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            playCount = new StatisticPlayCount(background: true, leftPadding: SongSelect.WEDGE_CONTENT_MARGIN, minSize: 50f)
            {
                Margin = new MarginPadding { Left = -SongSelect.WEDGE_CONTENT_MARGIN },
            };
            favouriteButton = new FavouriteButton();
            new ShearAligningWrapper(statusPill = new BeatmapSetOnlineStatusPill
            {
                ShowUnknownStatus = true,
                TextSize = OsuFont.Style.Caption1.Size,
                TextPadding = new MarginPadding { Horizontal = 6, Vertical = 1 }
            });
            Masking = true;
            CornerRadius = corner_radius + 2;
            BorderColour = Colour4.Black;
            BorderThickness = 2f;
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.8f,
                    Colour = new Colour4(34,34,34, 255),
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding { Top = 8, Left = 8 },
                    Spacing = new Vector2(0f, 4f),
                    Children = new Drawable[]
                    {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = OsuFont.Style.Heading2.Size,
                                    Margin = new MarginPadding { Left = 1f },
                                    Colour = new Colour4(185,185,185,255),
                                    Child = artistLink = new OsuHoverContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Child = artistLabel = new MarqueeContainer
                                        {
                                            OverflowSpacing = 50,
                                        }
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = OsuFont.Style.Title.Size,
                                    Margin = new MarginPadding { Bottom = -4f },
                                    Child = titleLink = new OsuHoverContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Child = titleLabel = new MarqueeContainer
                                        {
                                            OverflowSpacing = 50,
                                        }
                                    }
                                },
                                nameLine = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Colour = new Colour4(140, 140, 140, 255),
                                    Direction = FillDirection.Horizontal,
                                    Margin = new MarginPadding { Top = 8f, Bottom = 2f },
                                    Children = new Drawable[]
                                    {
                                        difficultyText = new TruncatingSpriteText
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Font = OsuFont.Style.Body,
                                        },
                                        mappedByText = new OsuSpriteText
                                        {
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Text = " mapped by ",
                                            Font = OsuFont.Style.Body,
                                        },
                                        mapperLink = new MapperLinkContainer
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Child = mapperText = new TruncatingSpriteText
                                            {
                                                Shadow = true,
                                                Font = OsuFont.Style.Body,
                                            },
                                        },
                                    },
                                },
                                new GridContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 30f,
                                    Padding = new MarginPadding { Right = 8 },
                                    ColumnDimensions = new[]
                                    {
                                        new Dimension(GridSizeMode.Relative, 0.32f),
                                        new Dimension(GridSizeMode.Relative, 0.06f),
                                        new Dimension(GridSizeMode.Relative, 0.62f),
                                    },
                                    Content = new[] {new Drawable[]
                                {
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Masking = true,
                                        CornerRadius = 16,
                                        BorderColour = Colour4.Black,
                                        BorderThickness = 2f,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = new Colour4(50, 50, 50, 255),
                                            },
                                            statisticsFlow = new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Horizontal,
                                                Children = new Drawable[]
                                                {
                                                    starRatingDisplay = new StarRatingDisplayH(default, animated: true),
                                                },
                                            },
                                        },
                                    },
                                    Empty(),
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Masking = true,
                                        CornerRadius = 16,
                                        BorderColour = Colour4.Black,
                                        BorderThickness = 2f,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = new Colour4(50, 50, 50, 255),
                                            },
                                            lengthStatistic = new Statistic(leftPadding: 10, maxSize: 0.46f)
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                Margin = new MarginPadding { Horizontal = 4f, },
                                            },
                                            statisticsFlow = new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Horizontal,
                                                RelativePositionAxes = Axes.X,
                                                Position = new Vector2(0.5f, 0f),
                                                Anchor = Anchor.TopLeft,
                                                Children = new Drawable[]
                                                {
                                                    new ShearAligningWrapper(new Box
                                                    {
                                                        RelativeSizeAxes = Axes.Y,
                                                        Width = 1f,
                                                        Shear = OsuGame.SHEAR,
                                                        EdgeSmoothness = new Vector2(1f),
                                                        Colour = new Colour4(0,0,0, 255),
                                                    }),
                                                    bpmStatistic = new Statistic()
                                                    {
                                                        TooltipText = BeatmapsetsStrings.ShowStatsBpm,
                                                    },
                                                },
                                            },

                                        },
                                    },
                                },
                            },
                                },
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Margin = new MarginPadding { Left = -SongSelect.WEDGE_CONTENT_MARGIN },
                            Padding = new MarginPadding { Right = -SongSelect.WEDGE_CONTENT_MARGIN },
                            Child = new DifficultyDisplay(),
                        },
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            working.BindValueChanged(_ => updateDisplay());
            ruleset.BindValueChanged(_ => updateDisplay());
            onlineLookupResult.BindValueChanged(_ => updateDisplay());

            mods.BindValueChanged(m =>
            {
                settingChangeTracker?.Dispose();

                updateLengthAndBpmStatistics();

                settingChangeTracker = new ModSettingChangeTracker(m.NewValue);
                settingChangeTracker.SettingChanged += _ => updateLengthAndBpmStatistics();
            });

            updateDisplay();

            statisticsFlow.AutoSizeDuration = 100;
            statisticsFlow.AutoSizeEasing = Easing.OutQuint;
        }

        protected override void PopIn()
        {
            this.MoveToX(0, SongSelect.ENTER_DURATION, Easing.OutQuint)
                .FadeIn(SongSelect.ENTER_DURATION / 3, Easing.In);
        }

        protected override void PopOut()
        {
            this.MoveToX(-150, SongSelect.ENTER_DURATION, Easing.OutQuint)
                .FadeOut(SongSelect.ENTER_DURATION / 3, Easing.In);
        }

        private void updateDisplay()
        {
            cancellationSource?.Cancel();
            cancellationSource = new CancellationTokenSource();
            var metadata = working.Value.Metadata;
            var beatmapInfo = working.Value.BeatmapInfo;

            statusPill.Status = beatmapInfo.Status;

            difficultyText.Text = working.Value.BeatmapInfo.DifficultyName;
            mapperLink.Action = () => linkHandler?.HandleLink(new LinkDetails(LinkAction.OpenUserProfile, working.Value.Metadata.Author));
            mapperText.Text = working.Value.Metadata.Author.Username;

            var titleText = new RomanisableString(metadata.TitleUnicode, metadata.Title);
            var fonttitle = OsuFont.Style.Title;
            titleLabel.CreateContent = () => new OsuSpriteText
            {
                Text = titleText,
                Shadow = true,
                Font = fonttitle,
            };
            titleLink.Action = () => songSelect?.AddToSearch(titleText.GetPreferred(localisation.CurrentParameters.Value.PreferOriginalScript));
            DisplayedTitle = titleText.ToString();

            var artistText = new RomanisableString(metadata.ArtistUnicode, metadata.Artist);
            artistLabel.CreateContent = () => new OsuSpriteText
            {
                Text = artistText,
                Shadow = true,
                Font = OsuFont.Style.Heading2,
            };
            artistLink.Action = () => songSelect?.AddToSearch(artistText.GetPreferred(localisation.CurrentParameters.Value.PreferOriginalScript));
            DisplayedArtist = artistText.ToString();

            starRatingDisplay.Current = (Bindable<StarDifficulty>)difficultyCache.GetBindableDifficulty(working.Value.BeatmapInfo, cancellationSource.Token, SongSelect.DIFFICULTY_CALCULATION_DEBOUNCE);

            updateLengthAndBpmStatistics();
            //updateOnlineDisplay();
        }

        private CancellationTokenSource? cancellationSource;
        private CancellationTokenSource? lengthBpmCancellationSource;

        private void updateLengthAndBpmStatistics()
        {
            lengthBpmCancellationSource?.Cancel();
            lengthBpmCancellationSource = new CancellationTokenSource();

            var token = lengthBpmCancellationSource.Token;

            Task.Run(() =>
            {
                var beatmapInfo = working.Value.BeatmapInfo;
                // This can take time as it is a synchronous task.
                var beatmap = working.Value.Beatmap;

                double rate = ModUtils.CalculateRateWithMods(mods.Value);

                int bpmMax = FormatUtils.RoundBPM(beatmap.ControlPointInfo.BPMMaximum, rate);
                int bpmMin = FormatUtils.RoundBPM(beatmap.ControlPointInfo.BPMMinimum, rate);
                int mostCommonBPM = FormatUtils.RoundBPM(60000 / beatmap.GetMostCommonBeatLength(), rate);

                double drainLength = Math.Round(beatmap.CalculateDrainLength() / rate);
                double hitLength = Math.Round(beatmapInfo.Length / rate);

                Schedule(() =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    lengthStatistic.Text = hitLength.ToFormattedDuration();
                    lengthStatistic.TooltipText = BeatmapsetsStrings.ShowStatsTotalLength(drainLength.ToFormattedDuration());

                    bpmStatistic.Text = bpmMin == bpmMax
                        ? $"{bpmMin}"
                        : LocalisableString.Interpolate($"{bpmMin}-{bpmMax} {SongSelectStrings.MostlyBPM(mostCommonBPM)}");
                });
            }, token);
        }

        protected override void Update()
        {
            base.Update();

            difficultyText.MaxWidth = Math.Max(nameLine.DrawWidth - mappedByText.DrawWidth - mapperText.DrawWidth - 20, 0);
        }

        private partial class MapperLinkContainer : OsuHoverContainer
        {
            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider? overlayColourProvider, OsuColour colours)
            {
                TooltipText = ContextMenuStrings.ViewProfile;
                IdleColour = overlayColourProvider?.Light2 ?? colours.Blue;
            }
        }
    }
}

