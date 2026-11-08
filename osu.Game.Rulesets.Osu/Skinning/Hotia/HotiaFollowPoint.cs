// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework.Constraints;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play;
using osu.Game.Skinning;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Skinning.Hotia
{
    public partial class HotiaFollowPoint : CompositeDrawable
    {
        [Resolved(canBeNull: true)]
        private IAnimationTimeReference animationTimeReference { get; set; }

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; }

        [Resolved(canBeNull: true)]
        private GameplayState? gameplayState { get; set; }

        private readonly IBindable<double> animationStartTime = new Bindable<double>();

        public HotiaFollowPoint()
        {
            Blending = BlendingParameters.Additive;
            AutoSizeAxes = Axes.Both;
            InternalChildren = new Drawable[]
            {
            new SpriteIcon
            {
                Icon = FontAwesome.Solid.Minus,
                Size = new Vector2(32),
                //Scale = new Vector2(1.7f, 0.7f),
                Colour = OsuColour.Gray(0.33f),
            },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (animationTimeReference != null)
                animationStartTime.BindTo(animationTimeReference.AnimationStartTime);
            animationStartTime.BindValueChanged(_ => restartAnimation(), true);

            var currentBeatmap = gameplayState != null ? gameplayState.Beatmap : beatmap.Value.Beatmap;
            float cs = currentBeatmap.Difficulty.CircleSize;
            //hotiaTODO: i absolutely don't know what i'm doing but who cares weeeeeeeeew \d^-^b/
            var circleSizeFactor = 0.9272f + cs * (0.25298f + cs * (-0.018235f + cs * (-0.002309f + cs * 0.000648f)));
            Scale = new Vector2(circleSizeFactor, 1f); //, 0.7f //1.75
        }

        private void restartAnimation()
        {
            ClearTransforms();
            Alpha = 1;

            using (BeginAbsoluteSequence(animationStartTime.Value))
            {
                this.FadeInFromZero(500, Easing.InQuint).Delay(600).FadeOut(400, Easing.OutCubic);
            }
        }
    }
}
