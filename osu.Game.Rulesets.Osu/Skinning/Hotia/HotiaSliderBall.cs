// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Skinning.Hotia
{
    public partial class HotiaSliderBall : CircularContainer
    {
        private readonly Box fill;
        private readonly SpriteIcon icon;

        private readonly Vector2 defaultIconScale = new Vector2(0.6f, 0.8f);

        private readonly IBindable<Color4> accentColour = new Bindable<Color4>();

        [Resolved(canBeNull: true)]
        private DrawableHitObject? parentObject { get; set; }

        public HotiaSliderBall()
        {
            Size = new Vector2(HotiaMainCirclePiece.OUTER_GRADIENT_SIZE);

            Masking = true;

            BorderThickness = HotiaMainCirclePiece.GRADIENT_THICKNESS;
            BorderColour = Color4.White;

            InternalChildren = new Drawable[]
            {
                fill = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                icon = new SpriteIcon
                {
                    Size = new Vector2(48),
                    Scale = defaultIconScale,
                    Icon = FontAwesome.Solid.AngleRight,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (parentObject != null)
                accentColour.BindTo(parentObject.AccentColour);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            accentColour.BindValueChanged(colour =>
            {
                fill.Colour = ColourInfo.GradientVertical(colour.NewValue, colour.NewValue.Darken(0.5f));
            }, true);

            if (parentObject != null)
            {
                parentObject.ApplyCustomUpdateState += updateStateTransforms;
                updateStateTransforms(parentObject, parentObject.State.Value);
            }
        }

        private void updateStateTransforms(DrawableHitObject drawableObject, ArmedState _)
        {
            // Gets called by slider ticks, tails, etc., leading to duplicated
            // animations which in this case have no visual impact (due to
            // instant fade) but may negatively affect performance
            if (drawableObject is not DrawableSlider)
                return;

            const float duration = 200;
            const float icon_scale = 0.9f;

            using (BeginAbsoluteSequence(drawableObject.StateUpdateTime))
            {
                this.FadeInFromZero(duration, Easing.OutQuint);
                icon.ScaleTo(0).Then().ScaleTo(defaultIconScale, duration, Easing.OutElasticHalf);
            }

            using (BeginAbsoluteSequence(drawableObject.HitStateUpdateTime))
            {
                // intentionally pile on an extra FadeOut to make it happen much faster
                this.FadeOut(duration / 4, Easing.OutQuint);
                icon.ScaleTo(defaultIconScale * icon_scale, duration, Easing.OutQuint);
            }
        }

        protected override void Update()
        {
            base.Update();

            //undo rotation on layers which should not be rotated.
            float appliedRotation = Parent!.Rotation;

            fill.Rotation = -appliedRotation;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (parentObject != null)
                parentObject.ApplyCustomUpdateState -= updateStateTransforms;
        }
    }
    public partial class HotiaFollowCircle : FollowCircle
    {
        private readonly CircularContainer circleContainer;
        private readonly Box circleFill;

        private readonly IBindable<Color4> accentColour = new Bindable<Color4>();

        [Resolved(canBeNull: true)]
        private DrawableHitObject? parentObject { get; set; }

        private float FOLLOW_AREA = 1.8f;

        public HotiaFollowCircle()
        {
            InternalChild = circleContainer = new CircularContainer
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                BorderThickness = 4,
                Blending = BlendingParameters.Additive,
                Alpha = 0.66f,
                Colour = Colour4.Cyan,
                Child = circleFill = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.33f,
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (parentObject != null)
                accentColour.BindTo(parentObject.AccentColour);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            accentColour.BindValueChanged(colour =>
            {
                circleContainer.BorderColour = ColourInfo.GradientVertical(colour.NewValue, colour.NewValue.Darken(0.5f));
                circleFill.Colour = ColourInfo.GradientVertical(colour.NewValue, colour.NewValue.Darken(0.5f));
            }, true);
        }

        protected override void OnSliderPress()
        {
            const float duration = 300f;

            if (Precision.AlmostEquals(0, Alpha))
                this.ScaleTo(1);

            this.ScaleTo(FOLLOW_AREA, 0, Easing.OutQuint)
                .FadeIn(0, Easing.OutQuint);
        }

        protected override void OnSliderRelease()
        {
            const float duration = 150;

            this.ScaleTo(FOLLOW_AREA * 1.2f, 0, Easing.OutQuint)
                .FadeTo(0, 0, Easing.OutQuint);
        }

        protected override void OnSliderEnd()
        {
            const float duration = 300;

            this.ScaleTo(1, 0, Easing.OutQuint)
                .FadeOut(0 / 2, Easing.OutQuint);
        }

        protected override void OnSliderTick()
        {
            if (Scale.X >= FOLLOW_AREA * 0.98f)
            {
                this.ScaleTo(FOLLOW_AREA * 1.08f, 40, Easing.OutQuint)
                    .Then()
                    .ScaleTo(FOLLOW_AREA, 200f, Easing.OutQuint);
            }
        }

        protected override void OnSliderBreak()
        {
        }
    }
}
