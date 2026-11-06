// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework.Internal;
using osu.Framework.IO.Network;
using osu.Game.Extensions;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.BeatmapListing;
using osu.Game.Rulesets;
using osu.Game.Scoring;

namespace osu.Game.Online.API.Requests
{
    public class SearchBeatmapSetsRequest : APIRequest<SearchBeatmapSetsResponse>
    {
        [CanBeNull]
        public IReadOnlyCollection<SearchGeneral> General { get; }

        public SearchCategory SearchCategory { get; }

        public SortCriteria SortCriteria { get; }

        public SortDirection SortDirection { get; }

        public SearchGenre Genre { get; }

        public SearchLanguage Language { get; }

        [CanBeNull]
        public IReadOnlyCollection<SearchExtra> Extra { get; }

        public SearchPlayed Played { get; }

        public SearchExplicit ExplicitContent { get; }

        [CanBeNull]
        public IReadOnlyCollection<ScoreRank> Ranks { get; }

        private readonly string query;
        private readonly RulesetInfo ruleset;
        private readonly Cursor cursor;

        private string directionString => SortDirection == SortDirection.Descending ? @"desc" : @"asc";

        public SearchBeatmapSetsRequest(
            string query,
            RulesetInfo ruleset,
            Cursor cursor = null,
            IReadOnlyCollection<SearchGeneral> general = null,
            SearchCategory searchCategory = SearchCategory.Any,
            SortCriteria sortCriteria = SortCriteria.Ranked,
            SortDirection sortDirection = SortDirection.Descending,
            SearchGenre genre = SearchGenre.Any,
            SearchLanguage language = SearchLanguage.Any,
            IReadOnlyCollection<SearchExtra> extra = null,
            IReadOnlyCollection<ScoreRank> ranks = null,
            SearchPlayed played = SearchPlayed.Any,
            SearchExplicit explicitContent = SearchExplicit.Hide)
        {
            this.query = query;
            this.ruleset = ruleset;
            this.cursor = cursor;

            General = general;
            SearchCategory = searchCategory;
            SortCriteria = sortCriteria;
            SortDirection = sortDirection;
            Genre = genre;
            Language = language;
            Extra = extra;
            Ranks = ranks;
            Played = played;
            ExplicitContent = explicitContent;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateBeatmapWebRequest();

            if (query != null)
                req.AddParameter("q", query);

            //if (General != null && General.Any())
            //    req.AddParameter("c", string.Join('.', General.Select(e => e.ToString().ToSnakeCase())));

            //if (ruleset.OnlineID >= 0)
            //    req.AddParameter("m", ruleset.OnlineID.ToString());

            //req.AddParameter("s", SearchCategory.ToString().ToLowerInvariant());

            //if (Genre != SearchGenre.Any)
            //    req.AddParameter("g", ((int)Genre).ToString());

            //if (Language != SearchLanguage.Any)
            //    req.AddParameter("l", ((int)Language).ToString());

            ////req.AddParameter("sort", $"{SortCriteria.ToString().ToLowerInvariant()}_{directionString}");

            //if (Extra != null && Extra.Any())
            //    req.AddParameter("e", string.Join('.', Extra.Select(e => e.ToString().ToLowerInvariant())));

            //if (Ranks != null && Ranks.Any())
            //    req.AddParameter("r", string.Join('.', Ranks.Select(r => r.ToString())));

            //if (Played != SearchPlayed.Any)
            //    req.AddParameter("played", Played.ToString().ToLowerInvariant());

            //req.AddParameter("nsfw", ExplicitContent == SearchExplicit.Show ? "true" : "false");

            if (cursor != null)
                req.AddCursor(cursor);
            return req;
        }

        protected override string Target => @"search"; //beatmapsets/
    }


    public class SearchBeatmapSetsRequestDirect : APIRequest<SearchBeatmapSetsResponseDirect>
    {
        [CanBeNull]
        public IReadOnlyCollection<SearchGeneral> General { get; }

        public SearchCategory SearchCategory { get; }

        public SortCriteria SortCriteria { get; }

        public SortDirection SortDirection { get; }

        public SearchGenre Genre { get; }

        public SearchLanguage Language { get; }

        [CanBeNull]
        public IReadOnlyCollection<SearchExtra> Extra { get; }

        public SearchPlayed Played { get; }

        public SearchExplicit ExplicitContent { get; }

        [CanBeNull]
        public IReadOnlyCollection<ScoreRank> Ranks { get; }

        public int Offset;

        public int LastResponseCount;

        private readonly string query;
        private readonly RulesetInfo ruleset;

        private string directionString => SortDirection == SortDirection.Descending ? @"desc" : @"asc";

        public SearchBeatmapSetsRequestDirect(
            string query,
            RulesetInfo ruleset,
            int currentPage = 0,
            int lastResponseCount = 0,
            IReadOnlyCollection<SearchGeneral> general = null,
            SearchCategory searchCategory = SearchCategory.Any,
            SortCriteria sortCriteria = SortCriteria.Ranked,
            SortDirection sortDirection = SortDirection.Descending,
            SearchGenre genre = SearchGenre.Any,
            SearchLanguage language = SearchLanguage.Any,
            IReadOnlyCollection<SearchExtra> extra = null,
            IReadOnlyCollection<ScoreRank> ranks = null,
            SearchPlayed played = SearchPlayed.Any,
            SearchExplicit explicitContent = SearchExplicit.Hide)
        {
            this.query = query;
            this.ruleset = ruleset;

            General = general;
            SearchCategory = searchCategory;
            SortCriteria = sortCriteria;
            SortDirection = sortDirection;
            Genre = genre;
            Language = language;
            Extra = extra;
            Ranks = ranks;
            Played = played;
            ExplicitContent = explicitContent;
            Offset = currentPage;
            LastResponseCount = lastResponseCount;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateBeatmapWebRequest();

            if (query != null)
                req.AddParameter("q", query);

            //if (General != null && General.Any())
            //    req.AddParameter("c", string.Join('.', General.Select(e => e.ToString().ToSnakeCase())));

            if (ruleset.OnlineID >= 0)
                req.AddParameter("mode", ruleset.ShortName.ToString());

            //ranked, approved, loved, qualified, pending, wip, graveyard
            switch (SearchCategory)
            {
                case SearchCategory.Ranked:
                    req.AddParameter("status", "ranked"); break;
                case SearchCategory.Leaderboard:
                    req.AddParameter("status", "approved"); break;
                case SearchCategory.Loved:
                    req.AddParameter("status", "loved"); break;
                case SearchCategory.Qualified:
                    req.AddParameter("status", "qualified"); break;
                case SearchCategory.Pending:
                    req.AddParameter("status", "pending"); break;
                case SearchCategory.Wip:
                    req.AddParameter("status", "wip"); break;
                case SearchCategory.Graveyard:
                    req.AddParameter("status", "graveyard"); break;
                case SearchCategory.Any:
                    break;
                default:
                    req.AddParameter("status", "ranked"); break;
            }
            //if (Genre != SearchGenre.Any)
            //    req.AddParameter("g", ((int)Genre).ToString());

            //if (Language != SearchLanguage.Any)
            //    req.AddParameter("l", ((int)Language).ToString());
            string criteria = "";
            //id, title, artist, creator, bpm, updated, ranked, submitted, favourites, difficulty
            switch (SortCriteria)
            {
                case SortCriteria.Plays:
                    criteria = "favourites";
                    break;
                case SortCriteria.Title:
                    criteria = "title";
                    break;
                case SortCriteria.Artist:
                    criteria = "artist";
                    break;
                case SortCriteria.Favourites:
                    criteria = "favourites";
                    break;
                case SortCriteria.Updated:
                    criteria = "updated";
                    break;
                case SortCriteria.Ranked:
                    criteria = "ranked";
                    break;
                case SortCriteria.Difficulty:
                    criteria = "difficulty";
                    break;
            }
            if (criteria.Length > 0)
                req.AddParameter("sort", $"{criteria}");
            req.AddParameter("order", $"{directionString}");

            //if (Extra != null && Extra.Any())
            //    req.AddParameter("e", string.Join('.', Extra.Select(e => e.ToString().ToLowerInvariant())));

            //if (Ranks != null && Ranks.Any())
            //    req.AddParameter("r", string.Join('.', Ranks.Select(r => r.ToString())));

            //if (Played != SearchPlayed.Any)
            //    req.AddParameter("played", Played.ToString().ToLowerInvariant());

            //req.AddParameter("nsfw", ExplicitContent == SearchExplicit.Show ? "true" : "false");

            req.AddParameter("page", ((int)(Offset * Math.Max(LastResponseCount, 1))).ToString());
            //hotiaTODO: accumulate "LastResponseCount"s

            //if (cursor != null)
            //    req.AddCursor(cursor);

            return req;
        }

        protected override string Target => @"search"; //beatmapsets/
    }

}
