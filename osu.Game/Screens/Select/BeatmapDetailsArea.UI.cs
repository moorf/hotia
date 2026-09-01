// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.Linq;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Resources.Localisation.Web;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Shapes;

namespace osu.Game.Graphics.UserInterface
{
    public partial class ToggleButton : RegularButton
    {
        private Sample? sampleOff;
        private Sample? sampleOn;

        /// <summary>
        /// Sheared toggle buttons by default play two samples when toggled: a click and a toggle (on/off).
        /// Sometimes this might be too much. Setting this to <c>false</c> will silence the toggle sound.
        /// </summary>
        protected virtual bool PlayToggleSamples => true;

        /// <summary>
        /// Whether this button is currently toggled to an active state.
        /// </summary>
        public BindableBool Active { get; } = new BindableBool();

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            sampleOn = audio.Samples.Get(@"UI/check-on");
            sampleOff = audio.Samples.Get(@"UI/check-off");
        }

        protected override HoverSounds CreateHoverSounds(HoverSampleSet sampleSet) => new HoverSounds(sampleSet);

        protected override void LoadComplete()
        {
            Active.BindDisabledChanged(disabled => Action = disabled ? null : Active.Toggle, true);
            Active.BindValueChanged(_ =>
            {
                UpdateActiveState();
                playSample();
            });

            UpdateActiveState();
            base.LoadComplete();
        }

        protected virtual void UpdateActiveState()
        {
            DarkerColour = Active.Value ? ColourProvider.HotiaHighlight1 : ColourProvider.HotiaBackground3;
            LighterColour = Active.Value ? ColourProvider.HotiaColour0 : ColourProvider.HotiaBackground1;
            TextColour = Active.Value ? ColourProvider.HotiaBackground6 : ColourProvider.HotiaContent1;
        }

        private void playSample()
        {
            if (PlayToggleSamples)
            {
                if (Active.Value)
                    sampleOn?.Play();
                else
                    sampleOff?.Play();
            }
        }
    }
}

namespace osu.Game.Graphics.UserInterface
{
    public partial class RegularButton : OsuClickableContainer
    {
        public const float DEFAULT_HEIGHT = 30;
        public const float CORNER_RADIUS = 12;
        public const float BORDER_THICKNESS = 2;

        public LocalisableString Text
        {
            get => text.Text;
            set => text.Text = value;
        }

        public float TextSize
        {
            get => text.Font.Size;
            set => text.Font = OsuFont.TorusAlternate.With(size: value);
        }

        public Colour4 DarkerColour
        {
            set
            {
                darkerColour = value;
                Scheduler.AddOnce(updateState);
            }
        }

        public Colour4 LighterColour
        {
            set
            {
                lighterColour = value;
                Scheduler.AddOnce(updateState);
            }
        }

        public Colour4 TextColour
        {
            set
            {
                textColour = value;
                Scheduler.AddOnce(updateState);
            }
        }

        [Resolved]
        protected OverlayColourProvider ColourProvider { get; private set; } = null!;

        private readonly Box background;
        private readonly OsuSpriteText text;

        private Colour4? darkerColour;
        private Colour4? lighterColour;
        private Colour4? textColour;

        private readonly Container backgroundLayer;
        private readonly Box flashLayer;

        protected readonly Container ButtonContent;

        /// <summary>
        /// Creates a new <see cref="ShearedButton"/>
        /// </summary>
        /// <remarks>
        /// By default, the button will have a height of <see cref="DEFAULT_HEIGHT"/>.
        /// Width should be set for each usage.
        /// </remarks>
        public RegularButton()
        {
            Height = DEFAULT_HEIGHT;

            //Shear = OsuGame.SHEAR;

            Content.Anchor = Content.Origin = Anchor.Centre;
            Content.CornerRadius = CORNER_RADIUS;
            Content.Masking = true;

            Children = new Drawable[]
            {
                backgroundLayer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    CornerRadius = CORNER_RADIUS,
                    Masking = true,
                    BorderThickness = BORDER_THICKNESS,
                    Child = background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                },
                ButtonContent = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    //Shear = -OsuGame.SHEAR,
                    Child = text = new OsuSpriteText
                    {
                        Font = OsuFont.TorusAlternate.With(size: 17),
                        Margin = new MarginPadding { Horizontal = 15 },
                    }
                },
                flashLayer = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Colour4.White.Opacity(0.9f),
                    Blending = BlendingParameters.Additive,
                    Alpha = 0,
                },
            };
        }

        protected override HoverSounds CreateHoverSounds(HoverSampleSet sampleSet) => new HoverClickSounds(sampleSet) { Enabled = { BindTarget = Enabled } };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Enabled.BindValueChanged(_ => Scheduler.AddOnce(updateState));

            updateState();
            FinishTransforms(true);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (Enabled.Value)
                flashLayer.FadeOutFromOne(800, Easing.OutQuint);

            return base.OnClick(e);
        }

        protected override bool OnHover(HoverEvent e)
        {
            Scheduler.AddOnce(updateState);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            Scheduler.AddOnce(updateState);
            base.OnHoverLost(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            Content.ScaleTo(0.9f, 2000, Easing.OutQuint);
            return true;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            Content.ScaleTo(1, 1000, Easing.OutElastic);
            base.OnMouseUp(e);
        }

        private void updateState()
        {
            var colourDark = darkerColour ?? ColourProvider.HotiaBackground3;
            var colourLight = lighterColour ?? ColourProvider.HotiaBackground1;
            var colourContent = textColour ?? ColourProvider.HotiaContent1;

            if (!Enabled.Value)
            {
                colourDark = colourDark.Darken(1f);
                colourLight = colourLight.Darken(1f);
            }
            else if (IsHovered)
            {
                colourDark = colourDark.Lighten(0.2f);
                colourLight = colourLight.Lighten(0.2f);
            }

            background.FadeColour(colourDark, 150, Easing.OutQuint);
            backgroundLayer.TransformTo(nameof(BorderColour), ColourInfo.GradientVertical(colourDark, colourLight), 150, Easing.OutQuint);

            if (!Enabled.Value)
                colourContent = colourContent.Opacity(0.6f);

            ButtonContent.FadeColour(colourContent, 150, Easing.OutQuint);
        }
    }
}

namespace osu.Game.Graphics.UserInterface
{
    public partial class HotiaDropdown<T> : Dropdown<T>, IKeyBindingHandler<GlobalAction>
    {
        private const float corner_radius = 12;

        protected override DropdownHeader CreateHeader() => new HotiaDropdownHeader();

        protected override DropdownMenu CreateMenu() => new HotiaDropdownMenu();

        public HotiaDropdown()
        {
            if (Header is HotiaDropdownHeader osuHeader)
                osuHeader.Dropdown = this;
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Repeat) return false;

            if (e.Action == GlobalAction.Back)
                return Back();

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        #region OsuDropdownMenu

        public partial class HotiaDropdownMenu : DropdownMenu, IKeyBindingHandler<GlobalAction>
        {
            public override bool HandleNonPositionalInput => State == MenuState.Open;

            private Sample? sampleOpen;
            private Sample? sampleClose;

            // todo: this uses the same styling as OsuMenu. hopefully we can just use OsuMenu in the future with some refactoring
            public HotiaDropdownMenu()
            {
                CornerRadius = corner_radius;

                MaskingContainer.CornerRadius = corner_radius;
                Alpha = 0;

                // todo: this uses the same styling as OsuMenu. hopefully we can just use OsuMenu in the future with some refactoring
                ItemsContainer.Padding = new MarginPadding(5);
            }

            [BackgroundDependencyLoader(true)]
            private void load(OverlayColourProvider? colourProvider, OsuColour colours, AudioManager audio)
            {
                BackgroundColour = colourProvider?.Background5 ?? Color4.Black;
                HoverColour = colourProvider?.Light4 ?? colours.PinkDarker;
                SelectionColour = colourProvider?.Background3 ?? colours.PinkDarker.Opacity(0.5f);

                sampleOpen = audio.Samples.Get(@"UI/dropdown-open");
                sampleClose = audio.Samples.Get(@"UI/dropdown-close");
            }

            // todo: this shouldn't be required after https://github.com/ppy/osu-framework/issues/4519 is fixed.
            private bool wasOpened;

            // todo: this uses the same styling as OsuMenu. hopefully we can just use OsuMenu in the future with some refactoring
            protected override void AnimateOpen()
            {
                wasOpened = true;
                this.FadeIn(300, Easing.OutQuint);
                sampleOpen?.Play();
            }

            protected override void AnimateClose()
            {
                if (wasOpened)
                {
                    this.FadeOut(300, Easing.OutQuint);
                    sampleClose?.Play();
                }
            }

            private Vector2? targetSize;

            // todo: this uses the same styling as OsuMenu. hopefully we can just use OsuMenu in the future with some refactoring
            protected override void UpdateSize(Vector2 newSize)
            {
                // TODO: should probably fix this at a framework level (this method is running every frame which can spam transforms)
                if (newSize == targetSize)
                    return;

                targetSize = newSize;

                if (Direction == Direction.Vertical)
                {
                    Width = newSize.X;
                    this.ResizeHeightTo(newSize.Y, 300, Easing.OutQuint);
                }
                else
                {
                    Height = newSize.Y;
                    this.ResizeWidthTo(newSize.X, 300, Easing.OutQuint);
                }
            }

            private Color4 hoverColour;

            public Color4 HoverColour
            {
                get => hoverColour;
                set
                {
                    hoverColour = value;
                    foreach (var c in Children.OfType<DrawableOsuDropdownMenuItem>())
                        c.BackgroundColourHover = value;
                }
            }

            private Color4 selectionColour;

            public Color4 SelectionColour
            {
                get => selectionColour;
                set
                {
                    selectionColour = value;
                    foreach (var c in Children.OfType<DrawableOsuDropdownMenuItem>())
                        c.BackgroundColourSelected = value;
                }
            }

            protected override Menu CreateSubMenu() => new OsuMenu(Direction.Vertical);

            protected override DrawableDropdownMenuItem CreateDrawableDropdownMenuItem(MenuItem item) => new DrawableOsuDropdownMenuItem(item)
            {
                BackgroundColourHover = HoverColour,
                BackgroundColourSelected = SelectionColour
            };

            protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) => new OsuScrollContainer(direction);

            public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
            {
                // logic copied from https://github.com/ppy/osu-framework/blob/baf865f1fd9e677310e7e432a7c6af99db7db914/osu.Framework/Graphics/UserInterface/Dropdown.cs#L702-L717
                var visibleMenuItemsList = VisibleMenuItems.ToList();

                if (visibleMenuItemsList.Count > 0)
                {
                    var currentPreselected = PreselectedItem;
                    int targetPreselectionIndex = visibleMenuItemsList.IndexOf(currentPreselected);

                    switch (e.Action)
                    {
                        case GlobalAction.SelectPrevious:
                            PreselectItem(targetPreselectionIndex - 1);
                            return true;

                        case GlobalAction.SelectNext:
                            PreselectItem(targetPreselectionIndex + 1);
                            return true;
                    }
                }

                return false;
            }

            public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
            {
            }

            #region DrawableOsuDropdownMenuItem

            public partial class DrawableOsuDropdownMenuItem : DrawableDropdownMenuItem
            {
                // IsHovered is used
                public override bool HandlePositionalInput => true;

                public new Color4 BackgroundColourHover
                {
                    get => base.BackgroundColourHover;
                    set
                    {
                        base.BackgroundColourHover = value;
                        updateColours();
                    }
                }

                public new Color4 BackgroundColourSelected
                {
                    get => base.BackgroundColourSelected;
                    set
                    {
                        base.BackgroundColourSelected = value;
                        updateColours();
                    }
                }

                private void updateColours()
                {
                    BackgroundColour = BackgroundColourHover.Opacity(0);

                    UpdateBackgroundColour();
                    UpdateForegroundColour();
                }

                public DrawableOsuDropdownMenuItem(MenuItem item)
                    : base(item)
                {
                    Foreground.Padding = new MarginPadding(2);
                    Foreground.AutoSizeAxes = Axes.Y;
                    Foreground.RelativeSizeAxes = Axes.X;

                    Masking = true;
                    CornerRadius = corner_radius;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    AddInternal(new HoverSounds());
                }

                protected override void UpdateBackgroundColour()
                {
                    Background.FadeColour(IsPreSelected ? BackgroundColourHover : BackgroundColourSelected, 100, Easing.OutQuint);

                    if (IsPreSelected || IsSelected)
                        Background.FadeIn(100, Easing.OutQuint);
                    else
                        Background.FadeOut(600, Easing.OutQuint);
                }

                protected override void UpdateForegroundColour()
                {
                    base.UpdateForegroundColour();

                    if (Foreground.Children.FirstOrDefault() is Content content)
                        content.Hovering = IsHovered;
                }

                protected override Drawable CreateContent() => new Content();

                protected new partial class Content : CompositeDrawable, IHasText
                {
                    public LocalisableString Text
                    {
                        get => Label.Text;
                        set => Label.Text = value;
                    }

                    public readonly OsuSpriteText Label;
                    public readonly SpriteIcon Chevron;

                    private const float chevron_offset = -3;

                    public Content()
                    {
                        RelativeSizeAxes = Axes.X;
                        AutoSizeAxes = Axes.Y;

                        InternalChildren = new Drawable[]
                        {
                            Chevron = new SpriteIcon
                            {
                                Icon = FontAwesome.Solid.ChevronRight,
                                Size = new Vector2(8),
                                Alpha = 0,
                                X = chevron_offset,
                                Y = 1,
                                Margin = new MarginPadding { Left = 3, Right = 3 },
                                Origin = Anchor.CentreLeft,
                                Anchor = Anchor.CentreLeft,
                            },
                            Label = new TruncatingSpriteText
                            {
                                Padding = new MarginPadding { Left = 15 },
                                Origin = Anchor.CentreLeft,
                                Anchor = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                            },
                        };
                    }

                    [BackgroundDependencyLoader(true)]
                    private void load(OverlayColourProvider? colourProvider)
                    {
                        Chevron.Colour = colourProvider?.Background5 ?? Color4.Black;
                    }

                    private bool hovering;

                    public bool Hovering
                    {
                        get => hovering;
                        set
                        {
                            if (value == hovering)
                                return;

                            hovering = value;

                            if (hovering)
                            {
                                Chevron.FadeIn(400, Easing.OutQuint);
                                Chevron.MoveToX(0, 400, Easing.OutQuint);
                            }
                            else
                            {
                                Chevron.FadeOut(200);
                                Chevron.MoveToX(chevron_offset, 200, Easing.In);
                            }
                        }
                    }
                }
            }

            #endregion
        }

        #endregion

        public partial class HotiaDropdownHeader : DropdownHeader
        {
            protected readonly SpriteText Text;

            protected override LocalisableString Label
            {
                get => Text.Text;
                set => Text.Text = value;
            }

            protected readonly SpriteIcon Chevron;

            public HotiaDropdown<T>? Dropdown { get; set; }

            public HotiaDropdownHeader()
            {
                Foreground.Padding = new MarginPadding(10);

                AutoSizeAxes = Axes.None;
                Margin = new MarginPadding { Bottom = 4 };
                CornerRadius = corner_radius;
                Height = 30;

                Foreground.Child = new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    ColumnDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            Text = new TruncatingSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                            },
                            Chevron = new SpriteIcon
                            {
                                Icon = FontAwesome.Solid.ChevronDown,
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Size = new Vector2(10),
                                Margin = new MarginPadding { Right = 2 },
                            },
                        }
                    }
                };

                AddInternal(new HoverClickSounds());
            }

            [Resolved]
            private OverlayColourProvider? colourProvider { get; set; }

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            protected override void LoadComplete()
            {
                base.LoadComplete();

                if (Dropdown != null)
                    Dropdown.Menu.StateChanged += _ => updateChevron();

                SearchBar.State.ValueChanged += _ => updateColour();
                Enabled.BindValueChanged(_ => updateColour());
                updateColour();
            }

            protected override bool OnHover(HoverEvent e)
            {
                updateColour();
                return false;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateColour();
            }

            private void updateColour()
            {
                bool hovered = Enabled.Value && IsHovered;
                var hoveredColour = colourProvider?.Light4 ?? colours.PinkDarker;
                var unhoveredColour = colourProvider?.Background5 ?? Color4.Black;

                Colour = Color4.White;
                Alpha = Enabled.Value ? 1 : 0.3f;

                if (SearchBar.State.Value == Visibility.Visible)
                {
                    Chevron.Colour = hovered ? hoveredColour.Lighten(0.5f) : Colour4.White;
                    Background.Colour = unhoveredColour;
                }
                else
                {
                    Chevron.Colour = Color4.White;
                    Background.Colour = hovered ? hoveredColour : unhoveredColour;
                }
            }

            private void updateChevron()
            {
                Debug.Assert(Dropdown != null);
                bool open = Dropdown.Menu.State == MenuState.Open;
                Chevron.ScaleTo(open ? new Vector2(1f, -1f) : Vector2.One, 300, Easing.OutQuint);
            }

            protected override DropdownSearchBar CreateSearchBar() => new OsuDropdownSearchBar
            {
                Padding = new MarginPadding { Right = 26 },
            };

            private partial class OsuDropdownSearchBar : DropdownSearchBar
            {
                protected override void PopIn() => this.FadeIn();

                protected override void PopOut() => this.FadeOut();

                protected override TextBox CreateTextBox() => new DropdownSearchTextBox
                {
                    FontSize = OsuFont.Default.Size,
                };

                private partial class DropdownSearchTextBox : OsuTextBox
                {
                    public DropdownSearchTextBox()
                    {
                        PlaceholderText = HomeStrings.SearchPlaceholder;
                    }

                    [BackgroundDependencyLoader]
                    private void load(OverlayColourProvider? colourProvider)
                    {
                        BackgroundUnfocused = colourProvider?.Background5 ?? new Color4(10, 10, 10, 255);
                        BackgroundFocused = colourProvider?.Background5 ?? new Color4(10, 10, 10, 255);
                    }

                    protected override void OnFocus(FocusEvent e)
                    {
                        base.OnFocus(e);
                        BorderThickness = 0;
                    }
                }
            }
        }
    }
}


