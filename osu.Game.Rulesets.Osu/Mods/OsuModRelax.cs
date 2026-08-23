// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using static osu.Game.Input.Handlers.ReplayInputHandler;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModRelax : ModRelax, IUpdatableByPlayfield, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToPlayer, IHasNoTimedInputs
    {
        public enum DelayMode
        {
            None = 0,
            Easy = 16,
            Normal = 25,
            Hard = 32,
            Insane = 38,
            Expert = 45,
            Extreme = 50
        }
        public override LocalisableString Description => @"You don't need to click. Give your clicking/tapping fingers a break from the heat of things.";

        public override Type[] IncompatibleMods =>
            base.IncompatibleMods.Concat(new[] { typeof(OsuModAutopilot), typeof(OsuModMagnetised), typeof(OsuModAlternate), typeof(OsuModSingleTap) }).ToArray();

        /// <summary>
        /// How early before a hitobject's start time to trigger a hit.
        /// </summary>
        public const float RELAX_LENIENCY = 12;

        private bool isDownState;
        private bool wasLeft;

        private OsuInputManager osuInputManager = null!;

        private ReplayState<OsuAction> state = null!;
        private double lastStateChangeTime;

        [SettingSource("Minimum time inside hitobject", "How long the cursor must stay inside a hitobject before Relax clicks it.")]
        public Bindable<DelayMode> DelayModeSetting { get; } = new Bindable<DelayMode>();

        private readonly Dictionary<DrawableHitCircle, double> hoverStartTimes = new Dictionary<DrawableHitCircle, double>();
        private DrawableOsuRuleset ruleset = null!;
        private IPressHandler pressHandler = null!;

        private bool hasReplay;
        private bool legacyReplay;

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            ruleset = (DrawableOsuRuleset)drawableRuleset;

            // grab the input manager for future use.
            osuInputManager = ruleset.KeyBindingInputManager;
        }

        public void ApplyToPlayer(Player player)
        {
            if (osuInputManager.ReplayInputHandler != null)
            {
                hasReplay = true;

                Debug.Assert(ruleset.ReplayScore != null);
                legacyReplay = ruleset.ReplayScore.ScoreInfo.IsLegacyScore;

                pressHandler = legacyReplay ? new LegacyReplayPressHandler(this) : new PressHandler(this);

                return;
            }

            pressHandler = new PressHandler(this);
            osuInputManager.AllowGameplayInputs = false;
        }

        public void Update(Playfield playfield)
        {
            if (hasReplay && !legacyReplay)
                return;

            bool requiresHold = false;
            bool requiresHit = false;

            double time = playfield.Clock.CurrentTime;

            foreach (var h in playfield.HitObjectContainer.AliveObjects.OfType<DrawableOsuHitObject>())
            {
                bool tooEarly = time < h.HitObject.StartTime - RELAX_LENIENCY;

                if (tooEarly && DelayModeSetting.Value == DelayMode.None)
                    break;

                if (h.IsHit || h.HitObject is IHasDuration hasEnd && time > hasEnd.EndTime)
                {
                    clearHoverTracking(h);
                    continue;
                }

                if (tooEarly)
                {
                    trackHover(h, time);
                    continue;
                }

                switch (h)
                {
                    case DrawableHitCircle circle:
                        handleHitCircle(circle);
                        break;

                    case DrawableSlider slider:
                        // Handles cases like "2B" beatmaps, where sliders may be overlapping and simply holding is not enough.
                        if (!slider.HeadCircle.IsHit)
                            handleHitCircle(slider.HeadCircle);
                        else
                            hoverStartTimes.Remove(slider.HeadCircle);

                        requiresHold |= slider.SliderInputManager.IsMouseInFollowArea(slider.Tracking.Value);
                        break;

                    case DrawableSpinner spinner:
                        requiresHold |= spinner.HitObject.SpinsRequired > 0;
                        break;
                }
            }

            if (requiresHit)
            {
                changeState(false);
                changeState(true);
            }

            if (requiresHold)
                changeState(true);
            else if (isDownState && time - lastStateChangeTime > AutoGenerator.KEY_UP_DELAY)
                changeState(false);

            void changeState(bool down)
            {
                if (isDownState == down)
                    return;

                isDownState = down;
                lastStateChangeTime = time;

                state = new ReplayState<OsuAction>
                {
                    PressedActions = new List<OsuAction>()
                };

                if (down)
                {
                    pressHandler.HandlePress(wasLeft);
                    wasLeft = !wasLeft;
                }
                else
                {
                    pressHandler.HandleRelease(wasLeft);
                }
            }

            void handleHitCircle(DrawableHitCircle circle)
            {
                if (DelayModeSetting.Value == DelayMode.None)
                {
                    if (!circle.HitArea.IsHovered)
                        return;

                    Debug.Assert(circle.HitObject.HitWindows != null);
                    requiresHit |= circle.HitObject.HitWindows.CanBeHit(time - circle.HitObject.StartTime);
                    return;
                }

                if (!circle.HitArea.IsHovered)
                {
                    hoverStartTimes.Remove(circle);
                    return;
                }

                if (!hoverStartTimes.TryGetValue(circle, out double hoverStartTime))
                    hoverStartTimes[circle] = hoverStartTime = time;
                var timeToHold = (int)DelayModeSetting.Value;
                if (time - hoverStartTime < timeToHold)
                    return;

                // This is technically already guaranteed by the outer loop, but kept here for clarity/safety.
                if (time < circle.HitObject.StartTime - RELAX_LENIENCY)
                    return;

                Debug.Assert(circle.HitObject.HitWindows != null);
                requiresHit |= circle.HitObject.HitWindows.CanBeHit(time - circle.HitObject.StartTime);
            }
        }

        private void trackHover(DrawableOsuHitObject drawableHitObject, double time)
        {
            switch (drawableHitObject)
            {
                case DrawableHitCircle circle:
                    trackHover(circle, time);
                    break;

                case DrawableSlider slider:
                    if (!slider.HeadCircle.IsHit)
                        trackHover(slider.HeadCircle, time);
                    break;
            }
        }

        private void trackHover(DrawableHitCircle circle, double time)
        {
            if (!circle.HitArea.IsHovered)
            {
                hoverStartTimes.Remove(circle);
                return;
            }

            if (!hoverStartTimes.ContainsKey(circle))
                hoverStartTimes[circle] = time;
        }

        private void clearHoverTracking(DrawableOsuHitObject drawableHitObject)
        {
            switch (drawableHitObject)
            {
                case DrawableHitCircle circle:
                    hoverStartTimes.Remove(circle);
                    break;

                case DrawableSlider slider:
                    hoverStartTimes.Remove(slider.HeadCircle);
                    break;
            }
        }

        private interface IPressHandler
        {
            void HandlePress(bool wasLeft);
            void HandleRelease(bool wasLeft);
        }

        private class PressHandler : IPressHandler
        {
            private readonly OsuModRelax mod;

            public PressHandler(OsuModRelax mod)
            {
                this.mod = mod;
            }

            public void HandlePress(bool wasLeft)
            {
                mod.state.PressedActions.Add(wasLeft ? OsuAction.LeftButton : OsuAction.RightButton);
                mod.state.Apply(mod.osuInputManager.CurrentState, mod.osuInputManager);
            }

            public void HandleRelease(bool wasLeft)
            {
                mod.state.Apply(mod.osuInputManager.CurrentState, mod.osuInputManager);
            }
        }

        // legacy replays do not contain key-presses with Relax mod, so they need to be triggered by themselves.
        private class LegacyReplayPressHandler : IPressHandler
        {
            private readonly OsuModRelax mod;

            public LegacyReplayPressHandler(OsuModRelax mod)
            {
                this.mod = mod;
            }

            public void HandlePress(bool wasLeft)
            {
                mod.osuInputManager.KeyBindingContainer.TriggerPressed(wasLeft ? OsuAction.LeftButton : OsuAction.RightButton);
            }

            public void HandleRelease(bool wasLeft)
            {
                // this intentionally releases right when `wasLeft` is true because `wasLeft` is set at point of press and not at point of release
                mod.osuInputManager.KeyBindingContainer.TriggerReleased(wasLeft ? OsuAction.RightButton : OsuAction.LeftButton);
            }
        }
    }
}
