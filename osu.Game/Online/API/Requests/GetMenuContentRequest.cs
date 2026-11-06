// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class GetMenuContentRequest : OsuJsonWebRequest<APIMenuContent>
    {
        public GetMenuContentRequest()
            : base(@"https://127.0.0.1/menu-content.json")
        {
        }
    }
}
