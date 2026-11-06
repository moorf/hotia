// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Online
{
    public class ProductionEndpointConfiguration : EndpointConfiguration
    {
        public ProductionEndpointConfiguration()
        {
            WebsiteUrl = APIUrl = @"https://localhost";//osu.hotia.org
            APIClientSecret = @"FGc9GAtyHzeQDshWP5Ah7dega8hJACAJpQtw6OXk";
            APIClientID = "5";
            SpectatorUrl = "https://localhost";
            MultiplayerUrl = "https://localhost";
            MetadataUrl = "https://localhost";
            BeatmapSubmissionServiceUrl = "https://localhost";
            BeatmapUrl = @"https://mirror.nekoha.moe";
        }
    }
}
