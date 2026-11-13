// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Screens.OnlinePlay;
using osuTK;

namespace osu.Game.Ai
{
    public sealed class AiScreenStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.AiScreen";

        /// <summary>
        /// "Beatmap generation wizard"
        /// </summary>
        public static LocalisableString Header => new TranslatableString(getKey(@"header"), @"Beatmap generation wizard");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }

    public partial class AiManagerOverlay : OsuFocusedOverlayContainer
    {
        [Cached]
        public readonly OverlayColourProvider ColourProvider;

        public AiManagerOverlay()
        {
            RelativeSizeAxes = Axes.Both;

            ColourProvider = new OverlayColourProvider(OverlayColourScheme.Lime);
        }
        private RoundedButton startGeneratingButton = null!;

        private GenerationOptionsForm genOptionsForm = null!;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        private GenerationOptions generationOptions = new GenerationOptions();

        private string path = "";

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            Content.Add(new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                Size = new Vector2(0.95f, 0.8f),
                Children = new Drawable[]
                {
                    new InputBlockingContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 14,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourProvider.Background6
                            },
                            new OsuScrollContainer(Direction.Vertical)
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Vertical = 20 },
                                ScrollbarVisible = false,
                                Child = new FillFlowContainer
                                {
                                    Direction = FillDirection.Vertical,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Spacing = new Vector2(5),
                                    Children = new Drawable[]
                                    {
                                        startGeneratingButton = new RoundedButton
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Size = new Vector2(1f, 60f),
                                            Text = "Start generating",
                                            Action = () => {
                                                genOptionsForm.ApplyTo(generationOptions);
                                                game.AiManager.Generate(generationOptions);
                                                Hide();
                                            }
                                        },
                                        genOptionsForm = new GenerationOptionsForm(),
                                    }
                                }
                            },
                        }
                    }
                }
            });

        }

        protected override bool OnClick(ClickEvent e)
        {
            if (State.Value == Visibility.Visible)
            {
                Hide();
                return true;
            }

            return base.OnClick(e);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
        }

        public void UpdatePath(string path)
        {
            this.path = path;
            Show();
        }

        protected override void PopIn()
        {
            this.FadeIn(200, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            base.PopOut();
            this.FadeOut(200, Easing.OutQuint);
        }

        public override void Show()
        {
            base.Show();
            genOptionsForm.AudioPathTextBox.Current.Value = path;
            //hotiaTODO: shortcut to show overlay, show audio path in onscreen notification when changed
        }
    }
}

