// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Game.Rulesets.Osu.Skinning.Argon;
using osu.Game.Rulesets.Osu.Skinning.Default;
using osu.Game.Skinning;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Skinning.Hotia
{
    public partial class HotiaSliderBody : PlaySliderBody
    {
        // Eventually this would be a user setting.
        public float BodyAlpha { get; init; } = 1;

        protected override void LoadComplete()
        {
            const float path_radius = HotiaMainCirclePiece.OUTER_GRADIENT_SIZE / 2;

            base.LoadComplete();

            AccentColourBindable.BindValueChanged(accent => BorderColour = accent.NewValue, true);
            ScaleBindable.BindValueChanged(scale => PathRadius = path_radius * scale.NewValue * 1.17f, true);

            // This border size thing is kind of weird, hey.
            const float intended_thickness = HotiaMainCirclePiece.GRADIENT_THICKNESS / (path_radius * 1.17f);

            BorderSize = intended_thickness / Default.DrawableSliderPath.BORDER_PORTION;
        }

        protected override Default.DrawableSliderPath CreateSliderPath() => new DrawableSliderPath();


        protected override Color4 GetBodyAccentColour(ISkinSource skin, Color4 hitObjectAccentColour)
        {
            return Color4.DarkCyan.Opacity(0.5f);
            //return base.GetBodyAccentColour(skin, hitObjectAccentColour).Opacity(BodyAlpha);
        }

        private partial class DrawableSliderPath : Default.DrawableSliderPath
        {
            protected override Color4 ColourAt(float position)
            {
                if (CalculatedBorderPortion != 0f && position <= CalculatedBorderPortion)
                    return BorderColour;

                return AccentColour.Darken(4);
            }
        }
    }
}
