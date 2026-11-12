// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.Globalization;
using System.Text;

namespace hotia.AiMapper
{
    /// <summary>
    /// hotiaTODO: fix code style autoformat
    /// </summary>
    public static class BeatmapWriter
    {
        private sealed class Group
        {
            public string Type = "";
            public int Time;
            public bool HasTime;
            public double? X;
            public double? Y;
            public double? Sv;
            public bool NewCombo;

            public List<int> Hitsounds = new();
            public List<int> SampleSets = new();
            public List<int> Additions = new();
            public List<int> Volumes = new();
        }

        private sealed record Redline(double Time, double BeatLength, int Meter);
        private sealed record Change(double Time, double? Sv, int? Volume);
        private sealed record ObjectLine(int Time, string Text);

        private readonly record struct Point(double X, double Y)
        {
            public static Point operator +(Point a, Point b) =>
                new(a.X + b.X, a.Y + b.Y);

            public static Point operator -(Point a, Point b) =>
                new(a.X - b.X, a.Y - b.Y);

            public static Point operator *(Point a, double b) =>
                new(a.X * b, a.Y * b);

            public double Length => Math.Sqrt(X * X + Y * Y);
        }

        private static readonly HashSet<string> types = new()
        {
            "circle", "spinner", "spinner_end",
            "slider_head", "bezier_anchor", "perfect_anchor",
            "catmull_anchor", "red_anchor", "last_anchor", "slider_end",
            "beat", "measure", "timing_point", "kiai",
            "hold_note", "hold_note_end", "drumroll", "drumroll_end",
            "denden", "denden_end", "scroll_speed_change"
        };

        public static string Write(
            List<Event> mapEvents,
            List<Event> timingEvents,
            GenerationOptions options,
            Action<string> log)
        {
            if (options.SliderMultiplier <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(options.SliderMultiplier));

            List<Redline> redlines = fitTiming(timingEvents);
            List<Group> groups = getGroups(mapEvents);

            var changes = new List<Change>();
            var objects = new List<ObjectLine>();

            Group? spinner = null;
            Group? head = null;
            Group? lastAnchor = null;

            var anchors = new List<Point>();
            string curve = "B";

            double lastX = 256;
            double lastY = 192;

            int first(List<int> values, int fallback = 0) =>
                values.Count == 0 ? fallback : values[0];

            void addObject(Group group, string text)
            {
                objects.Add(new ObjectLine(group.Time, text));

                if (group.Volumes.Count > 0)
                {
                    changes.Add(new Change(
                        group.Time, null, group.Volumes[0]));
                }
            }

            foreach (Group group in groups)
            {
                bool untimedGeometry = group.Type is
                    "bezier_anchor" or "perfect_anchor" or
                    "catmull_anchor" or "red_anchor" or "last_anchor";

                if (!group.HasTime && !untimedGeometry)
                    continue;

                group.X ??= lastX;
                group.Y ??= lastY;

                if (group.Type is
                    "circle" or "slider_head" or "bezier_anchor" or
                    "perfect_anchor" or "catmull_anchor" or
                    "red_anchor" or "last_anchor" or "slider_end")
                {
                    lastX = group.X.Value;
                    lastY = group.Y.Value;
                }

                string sample = $"{first(group.SampleSets)}:" +
                    $"{first(group.Additions)}:-1:0:";

                switch (group.Type)
                {
                    case "circle":
                        addObject(group,
                            $"{r(group.X.Value)},{r(group.Y.Value)}," +
                            $"{group.Time},{(group.NewCombo ? 5 : 1)}," +
                            $"{first(group.Hitsounds)},{sample}");
                        break;

                    case "spinner":
                        spinner = group;
                        break;

                    case "spinner_end":
                        if (spinner is not null && group.Time > spinner.Time)
                        {
                            objects.Add(new ObjectLine(
                                spinner.Time,
                                $"256,192,{spinner.Time},12," +
                                $"{first(group.Hitsounds)}," +
                                $"{group.Time},{sample}"));

                            if (group.Volumes.Count > 0)
                            {
                                changes.Add(new Change(
                                    group.Time, null, group.Volumes[0]));
                            }
                        }

                        spinner = null;
                        lastX = 256;
                        lastY = 192;
                        break;

                    case "slider_head":
                        head = group;
                        lastAnchor = null;
                        anchors.Clear();
                        curve = "B";
                        break;

                    case "bezier_anchor":
                    case "perfect_anchor":
                    case "catmull_anchor":
                    case "red_anchor":
                        if (head is null)
                            break;

                        if (anchors.Count == 0)
                        {
                            curve = group.Type switch
                            {
                                "perfect_anchor" => "P",
                                "catmull_anchor" => "C",
                                _ => "B"
                            };
                        }

                        anchors.Add(new Point(group.X.Value, group.Y.Value));

                        if (group.Type == "red_anchor")
                            anchors.Add(anchors[^1]);

                        break;

                    case "last_anchor":
                        if (head is not null)
                        {
                            if (lastAnchor is null)
                            {
                                anchors.Add(new Point(group.X.Value, group.Y.Value));
                            }

                            lastAnchor = group;
                        }
                        break;

                    case "slider_end":
                        if (head is null || lastAnchor is null || !lastAnchor.HasTime || anchors.Count == 0)
                        {
                            head = null;
                            lastAnchor = null;
                            anchors.Clear();
                            break;
                        }

                        double span = lastAnchor.Time - head.Time;
                        double duration = group.Time - head.Time;

                        if (span <= 0 || duration <= 0)
                        {
                            head = null;
                            lastAnchor = null;
                            anchors.Clear();
                            break;
                        }

                        int slides = Math.Max(
                            1, (int)Math.Round(duration / span, MidpointRounding.ToEven));

                        span = duration / slides;

                        var controlPoints = new List<Point>
                        {
                            new(head.X!.Value, head.Y!.Value)
                        };
                        controlPoints.AddRange(anchors);

                        if (curve == "P" && controlPoints.Count != 3)
                            curve = "B";

                        double maximumLength = curveLength(curve, controlPoints);

                        if (!double.IsFinite(maximumLength) ||
                            maximumLength < 0.001)
                        {
                            head = null;
                            lastAnchor = null;
                            anchors.Clear();
                            break;
                        }

                        Redline redline = redlineAt(redlines, head.Time);

                        double sv = head.Sv is > 0
                            ? head.Sv.Value
                            : maximumLength * redline.BeatLength / (100 * options.SliderMultiplier * span);

                        if (!double.IsFinite(sv) || sv <= 0)
                            throw new InvalidDataException("Invalid generated SV.");

                        double length =
                            sv * span * 100 *
                            options.SliderMultiplier / redline.BeatLength;

                        if (length > maximumLength)
                        {
                            double scale = length / maximumLength;
                            Point origin = controlPoints[0];

                            for (int i = 0; i < anchors.Count; i++)
                                anchors[i] = origin + (anchors[i] - origin) * scale;
                        }

                        changes.Add(new Change(head.Time, sv, null));

                        var nodeHitsounds = head.Hitsounds
                            .Concat(lastAnchor.Hitsounds.Skip(1))
                            .Concat(group.Hitsounds).ToList();

                        var nodeSets = head.SampleSets
                            .Concat(lastAnchor.SampleSets.Skip(1))
                            .Concat(group.SampleSets).ToList();

                        var nodeAdditions = head.Additions
                            .Concat(lastAnchor.Additions.Skip(1))
                            .Concat(group.Additions).ToList();

                        var nodeVolumes = head.Volumes
                            .Concat(lastAnchor.Volumes.Skip(1))
                            .Concat(group.Volumes).ToList();

                        while (nodeHitsounds.Count < slides + 1)
                            nodeHitsounds.Add(0);

                        while (nodeSets.Count < slides + 1)
                            nodeSets.Add(0);

                        while (nodeAdditions.Count < slides + 1)
                            nodeAdditions.Add(0);

                        string points = string.Join("|", anchors.Select(
                            point => $"{r(point.X)}:{r(point.Y)}"));

                        string edges = string.Join("|",
                            nodeHitsounds.Take(slides + 1));

                        string edgeSets = string.Join("|",
                            Enumerable.Range(0, slides + 1).Select(
                                i => $"{nodeSets[i]}:{nodeAdditions[i]}"));

                        string bodySample =
                            $"{first(lastAnchor.SampleSets)}:" +
                            $"{first(lastAnchor.Additions)}:-1:0:";

                        objects.Add(new ObjectLine(
                            head.Time,
                            $"{r(head.X.Value)},{r(head.Y.Value)}," +
                            $"{head.Time},{(head.NewCombo ? 6 : 2)}," +
                            $"{first(lastAnchor.Hitsounds)}," +
                            $"{curve}|{points},{slides},{f(length)}," +
                            $"{edges},{edgeSets},{bodySample}"));

                        for (int i = 0; i < Math.Min(slides + 1, nodeVolumes.Count); i++)
                        {
                            double time = Math.Round(
                                head.Time + span * i, MidpointRounding.ToEven);

                            changes.Add(new Change(time, null, nodeVolumes[i]));

                            if (i < slides && span > 6 &&
                                lastAnchor.Volumes.Count > 0 &&
                                lastAnchor.Volumes[0] != nodeVolumes[i])
                            {
                                changes.Add(new Change(
                                    time + 6, null, lastAnchor.Volumes[0]));
                            }
                        }
                        head = null;
                        lastAnchor = null;
                        anchors.Clear();
                        break;
                }
            }

            if (objects.Count == 0)
            {
                throw new InvalidDataException(
                    "The model produced no complete playable objects. " +
                    "No empty beatmap will be written.");
            }

            log(
                $"Writing {objects.Count} hit objects and " +
                $"{redlines.Count} BPM sections.");

            return serialize(
                options, redlines, changes,
                objects.OrderBy(x => x.Time).Select(x => x.Text));
        }

        private static List<Group> getGroups(List<Event> events)
        {
            var result = new List<Group>();
            var group = new Group();

            foreach (Event item in events)
            {
                switch (item.Type)
                {
                    case "t":
                        group.Time = item.Value;
                        group.HasTime = true;
                        break;

                    case "pos_x": group.X = item.Value; break;
                    case "pos_y": group.Y = item.Value; break;
                    case "scroll_speed": group.Sv = item.Value / 100.0; break;
                    case "new_combo": group.NewCombo = true; break;

                    case "hitsound":
                        group.Hitsounds.Add(item.Value % 8 * 2);
                        group.SampleSets.Add(item.Value / 8 % 3 + 1);
                        group.Additions.Add(item.Value / 24 % 3 + 1);
                        break;

                    case "volume":
                        group.Volumes.Add(item.Value);
                        break;

                    default:
                        if (types.Contains(item.Type))
                        {
                            group.Type = item.Type;
                            result.Add(group);
                            group = new Group();
                        }
                        break;
                }
            }

            return result;
        }

        private static List<Redline> fitTiming(List<Event> events)
        {
            var markers = getGroups(events)
                .Where(group => group.HasTime &&
                    group.Type is "beat" or "measure" or "timing_point")
                .OrderBy(group => group.Time)
                .ToList();

            if (markers.Count < 2)
            {
                throw new InvalidDataException("Insufficient generated timing markers to fit a BPM.");
            }

            var result = new List<Redline>();
            int start = 0;

            while (start < markers.Count - 1)
            {
                int end = start + 1;
                double chosenBeatLength = 0;

                for (int candidate = start + 1;
                     candidate < markers.Count;
                     candidate++)
                {
                    if (candidate > start + 1 &&
                        markers[candidate].Type == "timing_point")
                    {
                        break;
                    }

                    int beats = Enumerable.Range(
                            start + 1, candidate - start)
                        .Count(i => markers[i].Type != "timing_point");

                    if (beats == 0)
                        continue;

                    double beatLength =
                        (markers[candidate].Time - markers[start].Time) /
                        (double)beats;

                    if (beatLength <= 0)
                        continue;

                    int counter = 0;
                    bool fits = true;

                    for (int i = start + 1; i <= candidate; i++)
                    {
                        if (markers[i].Type != "timing_point")
                            counter++;

                        double expected =
                            markers[start].Time + counter * beatLength;

                        if (Math.Abs(expected - markers[i].Time) > 20)
                        {
                            fits = false;
                            break;
                        }
                    }

                    if (!fits)
                        break;

                    chosenBeatLength = beatLength;
                    end = candidate;
                }

                if (chosenBeatLength <= 0)
                {
                    int delta = markers[end].Time - markers[start].Time;

                    if (delta <= 0)
                    {
                        start++;
                        continue;
                    }

                    chosenBeatLength = delta;
                }

                double bpm = 60000 / chosenBeatLength;
                double roundedBpm = Math.Round(bpm, MidpointRounding.ToEven);

                if (roundedBpm > 0)
                {
                    double roundedLength = 60000 / roundedBpm;

                    if (Math.Abs(roundedLength - chosenBeatLength) *
                        Math.Max(1, end - start) <= 5)
                    {
                        chosenBeatLength = roundedLength;
                    }
                }

                int meter = 4;
                int previousMeasure = -1;
                int beatsSinceMeasure = 0;

                for (int i = start; i <= end; i++)
                {
                    if (i > start && markers[i].Type != "timing_point")
                        beatsSinceMeasure++;

                    if (markers[i].Type == "measure")
                    {
                        if (previousMeasure >= 0 && beatsSinceMeasure is >= 1 and <= 16)
                        {
                            meter = beatsSinceMeasure;
                            break;
                        }
                        previousMeasure = i;
                        beatsSinceMeasure = 0;
                    }
                }

                result.Add(new Redline(
                    markers[start].Time, chosenBeatLength, meter));

                start = end;
            }

            if (result.Count == 0)
                throw new InvalidDataException("Generated timing could not be fitted.");

            return result;
        }

        private static Redline redlineAt(List<Redline> redlines, double time)
        {
            for (int i = redlines.Count - 1; i >= 0; i--)
            {
                if (redlines[i].Time <= time)
                    return redlines[i];
            }

            return redlines[0];
        }

        private static double curveLength(
            string type, IReadOnlyList<Point> points)
        {
            var path = new List<Point>();

            void bezier(Point[] control, int depth)
            {
                bool flat = true;

                for (int i = 1; i < control.Length - 1; i++)
                {
                    if ((control[i - 1] - control[i] * 2 +
                         control[i + 1]).Length > 0.5)
                    {
                        flat = false;
                        break;
                    }
                }

                if (flat)
                {
                    path.Add(control[0]);
                    path.Add(control[^1]);
                    return;
                }

                if (depth >= 24)
                    throw new InvalidDataException(
                        "Generated Bezier curve exceeds subdivision limit.");

                var work = control.ToArray();
                var left = new Point[control.Length];
                var right = new Point[control.Length];

                for (int i = 0; i < control.Length; i++)
                {
                    left[i] = work[0];
                    right[control.Length - i - 1] =
                        work[control.Length - i - 1];

                    for (int j = 0; j < control.Length - i - 1; j++)
                        work[j] = (work[j] + work[j + 1]) * 0.5;
                }

                bezier(left, depth + 1);
                bezier(right, depth + 1);
            }

            if (type == "P" && points.Count == 3)
            {
                Point a = points[0];
                Point b = points[1];
                Point c = points[2];

                double d = 2 * (
                    a.X * (b.Y - c.Y) +
                    b.X * (c.Y - a.Y) +
                    c.X * (a.Y - b.Y));

                if (Math.Abs(d) > 1e-8)
                {
                    double aa = a.X * a.X + a.Y * a.Y;
                    double bb = b.X * b.X + b.Y * b.Y;
                    double cc = c.X * c.X + c.Y * c.Y;

                    var center = new Point(
                        (aa * (b.Y - c.Y) +
                         bb * (c.Y - a.Y) +
                         cc * (a.Y - b.Y)) / d,
                        (aa * (c.X - b.X) +
                         bb * (a.X - c.X) +
                         cc * (b.X - a.X)) / d);

                    double angleA = Math.Atan2(
                        a.Y - center.Y, a.X - center.X);

                    double angleC = Math.Atan2(
                        c.Y - center.Y, c.X - center.X);

                    double range = angleC - angleA;

                    while (range < 0)
                        range += Math.PI * 2;

                    double cross =
                        (c.X - a.X) * (b.Y - a.Y) -
                        (c.Y - a.Y) * (b.X - a.X);

                    if (cross > 0)
                        range = Math.PI * 2 - range;

                    return (a - center).Length * range;
                }
            }

            if (type == "C")
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Point p0 = i > 0 ? points[i - 1] : points[i];
                    Point p1 = points[i];
                    Point p2 = points[i + 1];
                    Point p3 = i + 2 < points.Count
                        ? points[i + 2]
                        : p2 * 2 - p1;

                    for (int j = 0; j <= 50; j++)
                    {
                        double t = j / 50.0;
                        double t2 = t * t;
                        double t3 = t2 * t;

                        path.Add((
                            p1 * 2 +
                            (p2 - p0) * t +
                            (p0 * 2 - p1 * 5 + p2 * 4 - p3) * t2 +
                            (p0 * -1 + p1 * 3 - p2 * 3 + p3) * t3
                        ) * 0.5);
                    }
                }
            }
            else
            {
                int start = 0;

                for (int i = 0; i < points.Count; i++)
                {
                    if (i == points.Count - 1 || points[i] == points[i + 1])
                    {
                        bezier(
                            points.Skip(start).Take(i - start + 1).ToArray(),
                            0);

                        start = i + 1;
                    }
                }
            }

            double length = 0;

            for (int i = 1; i < path.Count; i++)
                length += (path[i] - path[i - 1]).Length;

            return length;
        }

        private static string serialize(
            GenerationOptions options,
            List<Redline> redlines,
            List<Change> changes,
            IEnumerable<string> objects)
        {
            var timingRows = new List<(double Time, int Order, string Text)>();

            double firstTime = Math.Min(
                redlines[0].Time,
                changes.Count == 0
                    ? redlines[0].Time
                    : changes.Min(change => change.Time));

            if (firstTime < redlines[0].Time)
            {
                Redline first = redlines[0];
                double measure = first.BeatLength * first.Meter;

                double time = first.Time -
                    Math.Ceiling((first.Time - firstTime) / measure) * measure;

                redlines[0] = first with { Time = time };
            }

            foreach (Redline red in redlines)
            {
                timingRows.Add((
                    red.Time, 0,
                    $"{f(red.Time)},{f(red.BeatLength)}," +
                    $"{red.Meter},2,0,100,1,0"));
            }

            double sv = 1;
            int volume = 100;
            int redIndex = 0;

            foreach (var batch in changes.OrderBy(change => change.Time).GroupBy(change => change.Time))
            {
                while (redIndex < redlines.Count && redlines[redIndex].Time <= batch.Key)
                {
                    sv = 1;
                    volume = 100;
                    redIndex++;
                }

                foreach (Change change in batch)
                {
                    if (change.Sv.HasValue)
                        sv = change.Sv.Value;

                    if (change.Volume.HasValue)
                        volume = Math.Clamp(change.Volume.Value, 0, 100);
                }

                timingRows.Add((
                    batch.Key, 1,
                    $"{f(batch.Key)},{f(-100 / sv)},4,2,0," +
                    $"{volume},0,0"));
            }

            var text = new StringBuilder();

            text.AppendLine("osu file format v14");
            text.AppendLine();
            text.AppendLine("[General]");
            text.AppendLine($"AudioFilename: {clean(Path.GetFileName(options.AudioPath))}");
            text.AppendLine("AudioLeadIn: 0");
            text.AppendLine("PreviewTime: -1");
            text.AppendLine("Countdown: 0");
            text.AppendLine("SampleSet: All");
            text.AppendLine("StackLeniency: 0.7");
            text.AppendLine("Mode: 0");
            text.AppendLine("LetterboxInBreaks: 0");
            text.AppendLine("WidescreenStoryboard: 1");
            text.AppendLine();

            text.AppendLine("[Metadata]");
            text.AppendLine($"Title:{clean(options.Title)}");
            text.AppendLine($"TitleUnicode:{clean(options.Title)}");
            text.AppendLine($"Artist:{clean(options.Artist)}");
            text.AppendLine($"ArtistUnicode:{clean(options.Artist)}");
            text.AppendLine($"Creator:{clean(options.Creator)}");
            text.AppendLine($"Version:{clean(options.DifficultyName)}");
            text.AppendLine("Source:");
            text.AppendLine("Tags:Mapperatorinator -csharp");
            text.AppendLine();

            text.AppendLine("[Difficulty]");
            text.AppendLine($"HPDrainRate:{f(options.HpDrainRate)}");
            text.AppendLine($"CircleSize:{f(options.CircleSize)}");
            text.AppendLine($"OverallDifficulty:{f(options.OverallDifficulty)}");
            text.AppendLine($"ApproachRate:{f(options.ApproachRate)}");
            text.AppendLine($"SliderMultiplier:{f(options.SliderMultiplier)}");
            text.AppendLine($"SliderTickRate:{f(options.SliderTickRate)}");
            text.AppendLine();

            text.AppendLine("[Events]");
            text.AppendLine();

            text.AppendLine("[TimingPoints]");

            foreach (var row in timingRows
                .OrderBy(row => row.Time)
                .ThenBy(row => row.Order))
            {
                text.AppendLine(row.Text);
            }

            text.AppendLine();
            text.AppendLine("[HitObjects]");

            foreach (string line in objects)
                text.AppendLine(line);

            return text.ToString();
        }

        private static int r(double value) =>
            checked((int)Math.Round(value, MidpointRounding.ToEven));

        private static string f(double value) =>
            value.ToString("0.############", CultureInfo.InvariantCulture);

        private static string clean(string value) =>
            value.Replace("\r", " ").Replace("\n", " ");
    }

    public sealed record EventRange(
    string Type,
    int MinValue,
    int MaxValue,
    int StartId,
    bool IsInputOnly)
    {
        public int Count => checked(MaxValue - MinValue + 1);
        public int EndIdExclusive => checked(StartId + Count);
    }

    public readonly record struct Event(string Type, int Value);
}
