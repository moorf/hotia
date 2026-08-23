// Copyright (c) moorf. 2026.
// Released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Mods
{
    namespace osu.Game.Rulesets.Mods
    {
        public class ModColouredHit : Mod, IUpdatableByPlayfield, IApplicableToDrawableJudgement//IApplicableToDrawableHitObject
        {
            public override string Name => "Coloured Hit";
            public override string Acronym => "CH";
            public override IconUsage? Icon => OsuIcon.ModAlternate;
            public override LocalisableString Description => "See if you clicked too late right away!";

            private PlayfieldAdjustmentContainer playfieldAdjustmentContainer = null!;

            public virtual void Update(Playfield playfield)
            {
            }
            public void ApplyToDrawableJudgement(DrawableJudgement dj)
            {
                double t = dj.Result!.TimeOffset;
                bool positive = t > 0;

                float r = positive
                    ? (float)(255.0 - Math.Clamp(Math.Pow(Math.Max(t, 0.0f), 1.5), 0.0, 255.0)) / 255.0f
                    : (float)(255.0 - 7.0 * Math.Pow(Math.Max(Math.Clamp(t * 4.0f, 0.0f, 255.0f), 0.0f), 0.7)) / 255.0f;

                float g = positive
                    ? (float)(1.0 - Math.Pow(Math.Clamp(t, 0.0f, 96.0f) / 96.0, 2.0))
                    : (float)(255.0 - Math.Clamp(Math.Pow(Math.Max(-t, 0.0f), 1.5), 0.0, 255.0)) / 255.0f;

                float b = (float)(255.0f + Math.Clamp(t, -256.0f, 0.0f) * 1.5f) / 255.0f;

                Colour4 color = new Colour4(r: r, g: g, b: b, a: 1.0f);
                if (dj.Result!.IsHit)
                    dj.Colour = color;
            }
        }
    }
}
