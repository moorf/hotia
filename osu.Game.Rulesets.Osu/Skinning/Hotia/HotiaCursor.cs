// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Osu.UI.Cursor;
using osuTK;
using osuTK.Graphics;
using static Sentry.MeasurementUnit;

namespace osu.Game.Rulesets.Osu.Skinning.Hotia
{
    public partial class HotiaCursor : SkinnableCursor
    {
        public HotiaCursor()
        {
            RelativeSizeAxes = Axes.Both;

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            Scale = new Vector2(0.75f);
            InternalChildren = new[]
            {
                ExpandTarget = new CircularContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    BorderThickness = 0,
                    //BorderColour = ColourInfo.GradientVertical(Colour4.FromHex("FFFF00"), Colour4.FromHex("FF0000").Opacity(0f)),
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Glow,
                        Radius = 6,
                        Colour = new Color4(255, 255, 0, 50),
                    },
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 1f,
                            Colour = Colour4.FromHex("FFFF00"),
                        },
                        //new CircularContainer
                        //{
                        //    Origin = Anchor.Centre,
                        //    Anchor = Anchor.Centre,
                        //    RelativeSizeAxes = Axes.Both,
                        //    Masking = true,
                        //    BorderThickness = 2,
                        //    BorderColour = Color4.Yellow.Opacity(0.8f),
                        //    Children = new Drawable[]
                        //    {
                        //        new Box
                        //        {
                        //            RelativeSizeAxes = Axes.Both,
                        //            Alpha = 0,
                        //            AlwaysPresent = true,
                        //        },
                        //    },
                        //},
                    },
                },
                new Circle
                {
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Scale = new Vector2(0.7f),
                    Colour = new Color4(255, 255, 255, 255),
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Glow,
                        Radius = 5,
                        Colour = new Color4(255, 255, 255, 100),
                    },
                },
            };
        }
    }
    public partial class HotiaCursorTrail : CursorTrail
    {
        protected override float IntervalMultiplier => 0.3f;

        protected override float FadeExponent => 8;

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            Texture = textures.Get(@"Cursor/cursortrail");
            Scale = new Vector2(5.25f); //7 is for Cursor scale 1,
            Colour = Colour4.Yellow;
            Blending = BlendingParameters.Additive;

            Alpha = 0.8f;
        }
    }
}
