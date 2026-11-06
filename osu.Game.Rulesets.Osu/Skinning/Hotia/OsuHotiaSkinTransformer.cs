// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Game.Rulesets.Osu.Skinning.Argon;
using osu.Game.Rulesets.Scoring;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Osu.Skinning.Hotia
{
    public class OsuHotiaSkinTransformer : SkinTransformer
    {
        public OsuHotiaSkinTransformer(ISkin skin)
            : base(skin)
        {
        }

        public override Drawable? GetDrawableComponent(ISkinComponentLookup lookup)
        {
            bool isPro = true;

            switch (lookup)
            {
                case SkinComponentLookup<HitResult> resultComponent:
                    HitResult result = resultComponent.Component;

                    // This should eventually be moved to a skin setting, when supported.
                    if (isPro && (result == HitResult.Great || result == HitResult.Perfect))
                        return Drawable.Empty();

                    switch (result)
                    {
                        case HitResult.LargeTickHit:
                        case HitResult.SliderTailHit:
                            return null;

                        case HitResult.IgnoreMiss:
                        case HitResult.LargeTickMiss:
                            return new ArgonJudgementPieceSliderTickMiss(result);

                        default:
                            return new HotiaJudgementPiece(result);
                    }

                case OsuSkinComponentLookup osuComponent:
                    // TODO: Once everything is finalised, consider throwing UnsupportedSkinComponentException on missing entries.
                    switch (osuComponent.Component)
                    {
                        case OsuSkinComponents.HitCircle:
                            return new HotiaMainCirclePiece(true);

                        case OsuSkinComponents.SliderHeadHitCircle:
                            return new HotiaMainCirclePiece(false);

                        case OsuSkinComponents.SliderBody:
                            return new HotiaSliderBody
                            {
                                BodyAlpha = isPro ? 0.92f : 0.98f
                            };

                        case OsuSkinComponents.SliderBall:
                            return new HotiaSliderBall();

                        case OsuSkinComponents.SliderFollowCircle:
                            return new HotiaFollowCircle();

                        case OsuSkinComponents.SliderScorePoint:
                            return new ArgonSliderScorePoint();

                        case OsuSkinComponents.SpinnerBody:
                            return new ArgonSpinner();

                        case OsuSkinComponents.ReverseArrow:
                            return new ArgonReverseArrow();

                        case OsuSkinComponents.FollowPoint:
                            return new HotiaFollowPoint();

                        case OsuSkinComponents.Cursor:
                            return new HotiaCursor();

                        case OsuSkinComponents.CursorTrail:
                            return new HotiaCursorTrail();
                    }

                    break;
            }

            return base.GetDrawableComponent(lookup);
        }
    }
}
