// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Localisation;
using osu.Game.Resources.Localisation.Web;

namespace osu.Game.Overlays.BeatmapListing
{
    public enum SearchCategory
    {
        //| -2 = graveyard | -1 = wip | 0 = pending | 1 = ranked | 2 = approved | 3 = qualified | 4 = loved
        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusGraveyard))]
        Graveyard,

        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusWip))]
        //[Description("Has Leaderboard")]
        Wip,

        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusPending))]
        Pending,

        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusRanked))]
        Ranked,

        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusLeaderboard))]
        Leaderboard,

        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusQualified))]
        Qualified,

        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusLoved))]
        //[Description("Pending & WIP")]
        Loved,

        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusAny))]
        Any,

        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusFavourites))]
        Favourites,

        [LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusMine))]
        //[Description("My Maps")]
        Mine,
        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusAny))]
        //Any,

        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusLeaderboard))]
        //[Description("Has Leaderboard")]
        //Leaderboard,

        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusRanked))]
        //Ranked,

        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusQualified))]
        //Qualified,

        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusLoved))]
        //Loved,

        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusFavourites))]
        //Favourites,

        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusPending))]
        //[Description("Pending & WIP")]
        //Pending,

        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusWip))]
        //Wip,

        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusGraveyard))]
        //Graveyard,

        //[LocalisableDescription(typeof(BeatmapsStrings), nameof(BeatmapsStrings.StatusMine))]
        //[Description("My Maps")]
        //Mine,
    }
}
