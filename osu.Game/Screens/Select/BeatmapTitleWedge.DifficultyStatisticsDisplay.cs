// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Copyright (c) moorf. Modified 2026.
// Modifications released under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays;
using osuTK.Graphics;

namespace osu.Game.Screens.Select
{
    public partial class BeatmapTitleWedge
    {
        public partial class DifficultyStatisticsDisplay : CompositeDrawable
        {
            private const int rows = 4;
            private const int datacolumns = 2;
            private const int columns = 3; //2+empty

            private readonly bool autoSize;
            private readonly GridContainer statisticsGrid;

            private IReadOnlyList<StatisticDifficulty.Data> statistics = Array.Empty<StatisticDifficulty.Data>();

            public IReadOnlyList<StatisticDifficulty.Data> Statistics
            {
                get => statistics;
                set
                {
                    statistics = value;

                    if (IsLoaded)
                        updateStatistics();
                }
            }

            private Color4 accentColour;

            public Color4 AccentColour
            {
                get => accentColour;
                set
                {
                    if (accentColour == value)
                        return;

                    accentColour = value;

                    foreach (var cell in statisticsGrid.Content.SelectMany(row => row))
                    {
                        if (cell is StatisticDifficulty statistic)
                            statistic.AccentColour = value;
                    }
                }
            }

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public DifficultyStatisticsDisplay(bool autoSize = false)
            {
                this.autoSize = autoSize;
                Height = 153;
                RelativeSizeAxes = Axes.X;
                InternalChild = statisticsGrid = new GridContainer
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    RowDimensions = Enumerable.Range(0, rows).Select(_ => new Dimension(GridSizeMode.Absolute, 30)).ToArray(),
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Distributed),
                        new Dimension(GridSizeMode.Absolute, 10),
                        new Dimension(GridSizeMode.Distributed),
                    },
                };

            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                updateStatistics();
            }

            private void updateStatistics() => Scheduler.AddOnce(() =>
            {
                var cells = new Drawable[rows][];
                var createdStatistics = new List<StatisticDifficulty>();

                for (int row = 0; row < rows; row++)
                {
                    cells[row] = new Drawable[columns];

                    for (int col = 0; col < columns; col++)
                    {
                        if (col == 1)
                        {
                            cells[row][col] = Empty();
                            continue;
                        }
                        int dataCol = col == 0 ? 0 : 1;
                        int index = row * datacolumns + dataCol;

                        if (index < statistics.Count)
                        {
                            var statistic = new StatisticDifficulty
                            {
                                RelativeSizeAxes = Axes.X,
                                AccentColour = accentColour,
                                Value = statistics[index],
                            };

                            createdStatistics.Add(statistic);
                            cells[row][col] = statistic;
                        }
                        else
                            cells[row][col] = Empty();
                    }
                }

                statisticsGrid.Content = cells;
            });
        }
    }
}
