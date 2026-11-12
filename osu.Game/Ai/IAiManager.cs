// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System;
using osu.Game.Overlays.Notifications;
using osu.Game.Database;

namespace osu.Game.Ai
{
    public interface IAiManager : ICanAcceptFiles
    {
        Action<Notification>? PostNotification { get; set; }
        Action<string>? Generated { get; set; }
    }
}
