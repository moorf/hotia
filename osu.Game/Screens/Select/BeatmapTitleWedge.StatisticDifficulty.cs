// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Difficulty;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Select
{
    public partial class BeatmapTitleWedge
    {
        public partial class StatisticDifficulty : CompositeDrawable, IHasAccentColour, IHasCustomTooltip<RulesetBeatmapAttribute?>
        {
            private Data value = new Data(string.Empty, 0, 0, 0);

            public Data Value
            {
                get => value;
                set
                {
                    this.value = value;

                    if (IsLoaded)
                        updateDisplay();
                }
            }

            public float LabelWidth => labelText.DrawWidth;

            //private readonly OsuSpriteText labelText;
            private readonly OsuSpriteText valueText;
            //private readonly SpriteIcon valueIcon;
            private readonly Container bars;

            public Color4 AccentColour
            {
                get => new Color4(0, 0, 0, 0);
                set => new Color4(0, 0, 0, 0);
            }

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            public const float LABEL_BOX_WIDTH = 105f;
            private Container labelBox = null!;
            private OsuSpriteText labelText = null!;

            public StatisticDifficulty()
            {
                AutoSizeAxes = Axes.Y;
                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                    labelBox = new Container
                    {
                        Margin = new MarginPadding { Top = 5f },
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Masking = true,
                        CornerRadius = 10f,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = new Colour4(18,18,18,255),
                            },
                            labelText = new OsuSpriteText
                            {
                                RelativeSizeAxes = Axes.X,
                                Colour = Color4.WhiteSmoke,
                                Font = OsuFont.Style.Heading2,
                                Origin = Anchor.TopLeft,
                                Anchor = Anchor.TopLeft,
                                Padding = new MarginPadding(2f),
                                Position = new Vector2(6f, 0f),
                            },
                            valueText = new OsuSpriteText
                            {
                                Origin = Anchor.TopRight,
                                Anchor = Anchor.TopRight,
                                Padding = new MarginPadding(2f),
                                Position = new Vector2(-6f, 0f),
                                Font = OsuFont.Style.Heading2,
                                Alpha = 1f,
                            },
                        },
                    },

                    },
                };
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                labelText.Colour = colourProvider.Content1;
                valueText.Colour = colourProvider.Content1;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                updateDisplay();
            }

            private void updateDisplay()
            {
                valueText.Text = value.Content ?? value.AdjustedValue.ToLocalisableString("0.##");
                switch (value.Label.ToString())
                {
                    case "Accuracy":
                        labelText.Text = "OD";
                        break;
                    case "Approach Rate":
                        labelText.Text = "AR";
                        break;
                    case "Circle Size":
                        labelText.Text = "CS";
                        break;
                    case "Key Count":
                        labelText.Text = "KEYS";
                        break;
                    case "Scroll Speed":
                        labelText.Text = "SPD";
                        break;
                    case "HP Drain":
                        labelText.Text = "HP";
                        break;
                    case "Circles":
                        labelText.Text = "CIR";
                        break;
                    case "Sliders":
                        labelText.Text = "SLD";
                        break;
                    case "Spinners":
                        labelText.Text = "SPN";
                        break;
                    case "Drumrolls":
                        labelText.Text = "DRL";
                        break;
                    case "Swells":
                        labelText.Text = "SWL";
                        break;
                    case "Juice Streams":
                        labelText.Text = "JS";
                        break;
                    case "Fruits":
                        labelText.Text = "FRT";
                        break;
                    case "Banana Showers":
                        labelText.Text = "BNN";
                        break;
                    case "Notes":
                        labelText.Text = "NOTE";
                        break;
                    case "Hold Notes":
                        labelText.Text = "HLD";
                        break;
                    default:
                        labelText.Text = value.Label.ToUpper();
                        break;
                }
                if (value.Value == value.AdjustedValue)
                {

                    valueText.FadeColour(Color4.WhiteSmoke, 300, Easing.OutQuint);
                }
                else
                {
                    bool difficultyIncrease = value.Value < value.AdjustedValue;

                    if (difficultyIncrease)
                    {
                        valueText.FadeColour(colours.Red1, 300, Easing.OutQuint);
                    }
                    else
                    {

                        valueText.FadeColour(colours.Lime1, 300, Easing.OutQuint);
                    }
                }
            }

            public record Data(LocalisableString Label, float Value, float AdjustedValue, float Maximum, string? Content = null, RulesetBeatmapAttribute? BeatmapAttribute = null)
            {
                public Data(RulesetBeatmapAttribute attribute)
                    : this(attribute.Label, attribute.OriginalValue, attribute.AdjustedValue, attribute.MaxValue, BeatmapAttribute: attribute)
                {
                }
            }

            public ITooltip<RulesetBeatmapAttribute?> GetCustomTooltip() => new BeatmapAttributeTooltip();
            public RulesetBeatmapAttribute? TooltipContent => value.BeatmapAttribute;
        }
    }
}
