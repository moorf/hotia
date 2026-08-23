// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.Linq;
using osu.Game.IO;
using Realms;

namespace osu.Game.Models
{
    [MapTo("File")]
    public class RealmFile : RealmObject, IFileInfo
    {
        [PrimaryKey]
        public string Hash { get; set; } = string.Empty;

        public string OwnerHash { get; set; } = string.Empty;

        [Backlink(nameof(RealmNamedFileUsage.File))]
        public IQueryable<RealmNamedFileUsage> Usages { get; } = null!;
    }
}
