// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Ai
{
    public sealed class GenerationOptions
    {
        public string AudioPath { get; set; } = "";
        public string ModelDirectory { get; set; } = "";

        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";

        public string Creator { get; set; } = "Mapperatorinator";
        public string DifficultyName { get; set; } = "Extra";
        public string GameMode { get; set; } = "osu!standard";

        public double OverallDifficulty { get; set; } = 8.0;
        public double ApproachRate { get; set; } = 9.0;
        public double CircleSize { get; set; } = 4.0;
        public double HpDrainRate { get; set; } = 5.0;
        public double SliderMultiplier { get; set; } = 1.6;
        public double SliderTickRate { get; set; } = 1.0;
        public int? Seed { get; set; } = 0;

        public double? TargetStarRating { get; set; } = 5.3;
        public int Year { get; set; } = 2024;
        public int? MapperId { get; set; } = 4452992;
        public bool Hitsounded { get; set; } = true;

        public List<string> Descriptors { get; set; } = new();
        public List<string> NegativeDescriptors { get; set; } = new();

        public double CfgScale { get; set; } = 1.0;
        public bool DoSample { get; set; } = true;

        public int CudaDeviceId { get; set; } = 0;

        public bool VerifyDecoderCache { get; set; } = true;

        public static readonly Dictionary<string, int> DESCRIPTOR_IDX = new Dictionary<string, int>
        {
            ["style/messy"] = 1,
            ["expression/simple"] = 2,
            ["expression/chaotic"] = 3,
            ["style/geometric"] = 4,
            ["style/freeform"] = 5,
            ["style/grid snap"] = 6,
            ["style/hexgrid"] = 7,
            ["style/symmetrical"] = 8,
            ["gimmick/playfield constraint"] = 9,
            ["expression/old-style revival"] = 10,
            ["expression/repetition"] = 11,
            ["expression/progression"] = 12,
            ["expression/high contrast"] = 13,
            ["expression/improvisation"] = 86,
            ["skillset/tech"] = 111,
            ["tech/slider tech"] = 16,
            ["tech/finger control"] = 17,
            ["sliders/complex sv"] = 125,
            ["skillset/jumps"] = 19,
            ["jumps/sharp"] = 20,
            ["jumps/wide"] = 21,
            ["jumps/linear"] = 22,
            ["tech/aim control"] = 23,
            ["streams/flow aim"] = 24,
            ["streams/bursts"] = 25,
            ["skillset/streams"] = 26,
            ["streams/spaced streams"] = 27,
            ["streams/stamina"] = 129,
            ["streams/cutstreams"] = 29,
            ["skillset/reading"] = 30,
            ["reading/visually dense"] = 31,
            ["reading/overlaps"] = 32,
            ["skillset/precision"] = 98,
            ["skillset/alt"] = 34,
            ["meta/collab"] = 35,
            ["meta/multi-song"] = 38,
            ["meta/variable timing"] = 39,
            ["meta/time signatures"] = 40,
            ["style/clean"] = 41,
            ["sliders/complex slidershapes"] = 42,
            ["gimmick/aspire"] = 43,
            ["style/distance snap"] = 44,
            ["context/mapping contest"] = 45,
            ["context/tournament custom"] = 46,
            ["tech/complex snap"] = 136,
            ["expression/iNiS-style"] = 49,
            ["gimmick/2B"] = 50,
            ["expression/playfield usage"] = 51,
            ["gimmick/tag"] = 52,
            ["expression/difficulty spike"] = 53,
            ["style/avant-garde"] = 54,
            ["additions/keysounds"] = 55,
            ["gimmick/storyboard"] = 56,
            ["sliders/low sv"] = 57,
            ["sliders/high sv"] = 58,
            ["meta/mega collab"] = 59,
            ["gimmick/slider only"] = 60,
            ["gimmick/circle only"] = 61,
            ["additions/custom skin"] = 63,
            ["gimmick/video"] = 64,
            ["reading/perfect stacks"] = 66,
            ["gimmick/ninja spinners"] = 67,
            ["meta/accelerating bpm"] = 70,
            ["context/custom song"] = 71,
            ["gimmick/mirrored"] = 72,
            ["style/vocal"] = 73,
            ["style/tnt"] = 74,
            ["streams/speed"] = 75,
            ["style/double bpm"] = 76,
            ["style/finisher-heavy"] = 77,
            ["style/mono-heavy"] = 78,
            ["gimmick/reversed"] = 79,
            ["gimmick/yellow notes"] = 80,
            ["gimmick/barlines"] = 82,
            ["gimmick/memory"] = 83,
            ["style/taikosu"] = 84,
            ["meta/swing"] = 85,
            ["style/convert"] = 97,
            ["additions/hitsounds"] = 89,
            ["tech/flow"] = 91,
            ["tech/antiflow"] = 92,
            ["tech/jump"] = 93,
            ["gimmick/no hyperdashes"] = 94,
            ["gimmick/long sliders"] = 95,
            ["gimmick/spinner-heavy"] = 96,
            ["tech/wiggles"] = 99,
            ["tech/hyperwalks"] = 100,
            ["gimmick/dodge the beat"] = 101,
            ["style/jumpstream"] = 102,
            ["style/handstream"] = 103,
            ["style/quadstream"] = 104,
            ["style/chordjack"] = 105,
            ["style/longjack"] = 106,
            ["skillset/speedjack"] = 107,
            ["skillset/wristjack"] = 108,
            ["style/chordstream"] = 109,
            ["gimmick/delay"] = 110,
            ["style/mixed rice"] = 112,
            ["style/LN coordination"] = 113,
            ["style/LN release"] = 114,
            ["gimmick/LN inverse"] = 115,
            ["style/LN density"] = 116,
            ["style/LN mixed "] = 117,
            ["style/generic hybrid"] = 118,
            ["tech/technical hybrid"] = 119,
            ["style/tiebreaker"] = 120,
            ["style/o2jam"] = 121,
            ["style/N+1"] = 122,
            ["style/dump"] = 123,
            ["jumps/squares"] = 130,
            ["jumps/triangles"] = 131,
            ["jumps/cross-screen"] = 132,
            ["jumps/stamina"] = 133,
            ["skillset/gimmick"] = 137,
            ["jumps/stars"] = 138,
            ["jumps/back and forth"] = 139,
            ["jumps/freeform"] = 140,
            ["streams/doubles"] = 141,
            ["streams/quads"] = 142,
            ["expression/inspo"] = 143,
            ["expression/conceptual"] = 144,
            ["additions/combo colours"] = 145,
        };
    }
}
