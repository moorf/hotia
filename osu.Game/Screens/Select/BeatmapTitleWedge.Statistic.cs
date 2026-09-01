// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Select
{
    public partial class BeatmapTitleWedge
    {
        public partial class Statistic : CompositeDrawable, IHasTooltip
        {
            private readonly IconUsage icon;
            private readonly bool background;
            private readonly float leftPadding;
            private readonly float? minSize;
            private readonly float maxSize;

            private TruncatingSpriteText valueText = null!;
            private LoadingSpinner loading = null!;

            private LocalisableString? text;

            public LocalisableString? Text
            {
                get => text;
                set
                {
                    text = value;
                    Scheduler.AddOnce(updateDisplay);
                }
            }

            public LocalisableString TooltipText { get; set; }

            public Statistic(IconUsage icon = default, bool background = false, float leftPadding = 10f, float? minSize = null, float maxSize = 0.5f)
            {
                this.icon = icon;
                this.background = background;
                this.leftPadding = leftPadding;
                this.minSize = minSize;
                this.maxSize = maxSize;
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                Masking = true;
                CornerRadius = 5;
                Shear = background ? OsuGame.SHEAR : Vector2.Zero;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = background ? 0.2f : 0f,
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Margin = new MarginPadding { Left = background ? leftPadding : 0, Right = background ? 10f : 0f, Vertical = 6f },
                        Padding = new MarginPadding { Right = 14f },
                        Spacing = new Vector2(4f, 0f),
                        Shear = background ? -OsuGame.SHEAR : Vector2.Zero,
                        Children = new Drawable[]
                        {

                            icon.Icon != default(IconUsage).Icon ? new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Icon = icon,
                                Size = new Vector2(OsuFont.Style.Heading2.Size),
                                Colour = colourProvider.Content2,
                            } : Empty(),
                            new Container
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Children = new Drawable[]
                                {
                                    loading = new LoadingSpinner
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(14f),
                                        State = { Value = Visibility.Visible },
                                    },
                                    new Container
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Child = valueText = new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Font = OsuFont.Style.Heading2,
                                            Colour = new Colour4(200,200,200,255),
                                            Margin = new MarginPadding { Bottom = 2f },
                                            AlwaysPresent = true,
                                            MaxWidth = maxSize,
                                        },
                                    }
                                }
                            },
                        },
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Scheduler.AddOnce(updateDisplay);
            }

            private void updateDisplay()
            {
                loading.State.Value = text != null ? Visibility.Hidden : Visibility.Visible;

                if (text != null)
                {
                    valueText.Text = text.Value;
                    valueText.FadeIn(120, Easing.OutQuint);
                }
                else
                    valueText.FadeOut(120, Easing.OutQuint);
            }
        }
    }
}
