// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.Text.Json;

namespace hotia.AiMapper.Inference
{
    public sealed class Tokenizer
    {
        public const int PAD_ID = 0;
        public const int SOS_ID = 1;
        public const int EOS_ID = 2;

        public int OutputVocabularySize { get; }
        public int InputVocabularySize { get; }

        public IReadOnlyDictionary<string, int> ContextSos { get; }
        public IReadOnlyDictionary<string, int> ContextEos { get; }

        public IReadOnlyDictionary<string, int> DescriptorIndices { get; }
        public IReadOnlyDictionary<int, int> MapperIndices { get; }

        public IReadOnlyList<EventRange> Ranges => ranges;

        private readonly List<EventRange> ranges = new();
        private readonly Dictionary<string, EventRange> byType = new(StringComparer.Ordinal);

        private Tokenizer(JsonElement root)
        {
            OutputVocabularySize = root.GetProperty("vocab_size_out").GetInt32();
            InputVocabularySize = root.GetProperty("vocab_size_in").GetInt32();

            ContextSos = readIntDictionary(root, "context_sos");
            ContextEos = readIntDictionary(root, "context_eos");

            DescriptorIndices = readIntDictionary(root, "descriptor_idx");

            MapperIndices = readIntDictionary(root, "mapper_idx")
                .ToDictionary(
                    pair => int.Parse(
                        pair.Key,
                        System.Globalization.CultureInfo.InvariantCulture),
                    pair => pair.Value);

            int cursor = root.GetProperty("offset").GetInt32();

            readRanges(root, "event_ranges", false, ref cursor);

            if (cursor != OutputVocabularySize)
            {
                throw new InvalidDataException(
                    $"Output vocabulary mismatch: ranges end at {cursor}, but vocab_size_out is {OutputVocabularySize}.");
            }

            readRanges(root, "input_event_ranges", true, ref cursor);

            if (cursor != InputVocabularySize)
            {
                throw new InvalidDataException(
                    $"Input vocabulary mismatch: ranges end at {cursor}, but vocab_size_in is {InputVocabularySize}.");
            }

            if (root.TryGetProperty("event_start", out var starts))
            {
                foreach (var range in ranges)
                {
                    if (!starts.TryGetProperty(range.Type, out var start) ||
                        start.GetInt32() != range.StartId)
                    {
                        throw new InvalidDataException($"Inconsistent start ID for event type '{range.Type}'.");
                    }
                }
            }

            if (root.TryGetProperty("event_end", out var ends))
            {
                foreach (var range in ranges)
                {
                    if (!ends.TryGetProperty(range.Type, out var end) ||
                        end.GetInt32() != range.EndIdExclusive)
                    {
                        throw new InvalidDataException($"Inconsistent end ID for event type '{range.Type}'.");
                    }
                }
            }
        }

        public static Tokenizer Load(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return new Tokenizer(document.RootElement);
        }

        public EventRange GetRange(string type)
        {
            if (byType.TryGetValue(type, out var range))
                return range;
            throw new ArgumentException($"Tokenizer has no event type '{type}'.", nameof(type));
        }

        public int Encode(Event item)
        {
            var range = GetRange(item.Type);
            if (item.Value < range.MinValue || item.Value > range.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(item),
                    $"{item.Type} value {item.Value} is outside " +
                    $"[{range.MinValue}, {range.MaxValue}].");
            }
            return checked(range.StartId + item.Value - range.MinValue);
        }

        public bool TryDecode(long tokenId, out Event item)
        {
            foreach (var range in ranges)
            {
                if (tokenId >= range.StartId &&
                    tokenId < range.EndIdExclusive)
                {
                    item = new Event(range.Type, checked(range.MinValue + (int)tokenId - range.StartId));
                    return true;
                }
            }
            // PAD, SOS, EOS
            item = default;
            return false;
        }

        public Event Decode(long tokenId)
        {
            if (TryDecode(tokenId, out var item))
                return item;
            throw new ArgumentOutOfRangeException(nameof(tokenId), $"Token {tokenId} does not map to an event.");
        }

        public List<Event> DecodeWindow(IEnumerable<long> tokens, int frameTimeMilliseconds)
        {
            var result = new List<Event>();
            foreach (long token in tokens)
            {
                if (token == EOS_ID)
                    break;
                if (!TryDecode(token, out var item))
                    continue;

                if (item.Type == "t")
                {
                    int halfStep = item.Value >= 0 ? 5 : 0;
                    item = item with
                    {
                        Value = checked(
                            frameTimeMilliseconds + item.Value * 10 + halfStep)
                    };
                }
                result.Add(item);
            }
            return result;
        }

        public int EncodeForWindow(Event item, int frameTimeMilliseconds)
        {
            if (item.Type != "t")
                return Encode(item);

            var range = GetRange("t");

            int relativeStep = checked(item.Value - frameTimeMilliseconds) / 10;
            relativeStep = Math.Clamp(
                relativeStep, range.MinValue, range.MaxValue);

            return Encode(new Event("t", relativeStep));
        }

        private void readRanges(JsonElement root, string propertyName, bool inputOnly, ref int cursor)
        {
            foreach (var item in root.GetProperty(propertyName).EnumerateArray())
            {
                string type = item.GetProperty("type").GetString()
                    ?? throw new InvalidDataException("Missing event type.");

                int minimum = item.GetProperty("min_value").GetInt32();
                int maximum = item.GetProperty("max_value").GetInt32();

                if (maximum < minimum)
                    throw new InvalidDataException($"Invalid range for '{type}'.");

                var range = new EventRange(
                    type, minimum, maximum, cursor, inputOnly);

                ranges.Add(range);

                if (!byType.TryAdd(type, range))
                    throw new InvalidDataException($"Duplicate event type '{type}'.");

                cursor = range.EndIdExclusive;
            }
        }

        private static Dictionary<string, int> readIntDictionary(JsonElement root, string propertyName)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);

            if (root.TryGetProperty(propertyName, out var property))
            {
                foreach (var item in property.EnumerateObject())
                    result.Add(item.Name, item.Value.GetInt32());
            }

            return result;
        }
    }
}
