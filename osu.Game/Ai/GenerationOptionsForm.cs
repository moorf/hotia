// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;

namespace osu.Game.Ai
{
    public partial class GenerationOptionsForm : GridContainer
    {
        public FormTextBox AudioPathTextBox = null!;
        protected FormTextBox ModelDirectoryTextBox = null!;
        protected FormTextBox TitleTextBox = null!;
        protected FormTextBox ArtistTextBox = null!;
        protected FormTextBox CreatorTextBox = null!;
        protected FormTextBox DifficultyNameTextBox = null!;
        protected FormTextBox GameModeTextBox = null!;

        protected FormSliderBar<double> OverallDifficultyTextBox = null!;
        protected FormSliderBar<double> ApproachRateTextBox = null!;
        protected FormSliderBar<double> CircleSizeTextBox = null!;
        protected FormSliderBar<double> HpDrainRateTextBox = null!;
        protected FormSliderBar<double> SliderMultiplierTextBox = null!;
        protected FormNumberBox SliderTickRateTextBox = null!;
        protected FormNumberBox SeedTextBox = null!;

        protected FormSliderBar<double> TargetStarRatingNumberBox = null!;
        protected FormNumberBox YearNumberBox = null!;
        protected FormNumberBox MapperIdNumberBox = null!;
        protected FormCheckBox HitsoundedCheckBox = null!;
        protected LabelledDropdown<string> DescriptorsDropdown = null!;
        protected LabelledDropdown<string> NegativeDescriptorsDropdown = null!;
        protected FormNumberBox CfgScaleNumberBox = null!;
        protected FormCheckBox DoSampleCheckBox = null!;
        protected FormNumberBox CudaDeviceIdNumberBox = null!;
        protected FormCheckBox VerifyDecoderCacheCheckBox = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            GenerationOptions genOptions = new GenerationOptions();
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            AudioPathTextBox = new FormTextBox
            {
                Caption = nameof(GenerationOptions.AudioPath),
                Current = new Bindable<string>(genOptions.AudioPath),
            };
            ModelDirectoryTextBox = new FormTextBox
            {
                Caption = nameof(GenerationOptions.ModelDirectory),
                Current = new Bindable<string>(genOptions.ModelDirectory),
            };
            GameModeTextBox = new FormTextBox
            {
                Caption = nameof(GenerationOptions.GameMode),
                Current = new Bindable<string>(genOptions.GameMode),
            };
            DoSampleCheckBox = new FormCheckBox
            {
                Caption = nameof(GenerationOptions.DoSample),
                Current = new Bindable<bool>(genOptions.DoSample),
            };
            CudaDeviceIdNumberBox = new FormNumberBox
            {
                Caption = nameof(GenerationOptions.CudaDeviceId),
                Current = new Bindable<string>(genOptions.CudaDeviceId.ToString()),
            };
            VerifyDecoderCacheCheckBox = new FormCheckBox
            {
                Caption = nameof(GenerationOptions.VerifyDecoderCache),
                Current = new Bindable<bool>(genOptions.VerifyDecoderCache),
            };
            RowDimensions = Enumerable.Range(0, 5).Select(_ => new Dimension(GridSizeMode.Absolute, 60)).ToArray();
            ColumnDimensions = new[]
            {
                    new Dimension(GridSizeMode.Distributed),
                    new Dimension(GridSizeMode.Distributed),
                    new Dimension(GridSizeMode.Distributed),
                    new Dimension(GridSizeMode.Distributed),
                };
            Content = new Drawable[][]
            {
                    new Drawable[]
                    {
                        TitleTextBox = new FormTextBox
                        {
                            Caption = nameof(GenerationOptions.Title),
                            Current = new Bindable<string>(genOptions.Title),
                        },
                        ArtistTextBox = new FormTextBox
                        {
                            Caption = nameof(GenerationOptions.Artist),
                            Current = new Bindable<string>(genOptions.Artist),
                        },
                        CreatorTextBox = new FormTextBox
                        {
                            Caption = nameof(GenerationOptions.Creator),
                            Current = new Bindable<string>(genOptions.Creator),
                        },
                        DifficultyNameTextBox = new FormTextBox
                        {
                            Caption = nameof(GenerationOptions.DifficultyName),
                            Current = new Bindable<string>(genOptions.DifficultyName),
                        }
                    },
                    new Drawable[]
                    {
                        OverallDifficultyTextBox = new FormSliderBar<double>
                        {
                            Caption = nameof(GenerationOptions.OverallDifficulty),
                            Current = new BindableDouble(genOptions.OverallDifficulty)
                            {
                                MinValue = 0,
                                MaxValue = 10,
                                Precision = 0.1,
                            },
                        },
                        ApproachRateTextBox = new FormSliderBar<double>
                        {
                            Caption = nameof(GenerationOptions.ApproachRate),
                            Current = new BindableDouble(genOptions.ApproachRate)
                            {
                                MinValue = 0,
                                MaxValue = 10,
                                Precision = 0.1,
                            },
                        },
                        CircleSizeTextBox = new FormSliderBar<double>
                        {
                            Caption = nameof(GenerationOptions.CircleSize),
                            Current = new BindableDouble(genOptions.CircleSize)
                            {
                                MinValue = 0,
                                MaxValue = 10,
                                Precision = 0.1,
                            },
                        },
                        HpDrainRateTextBox = new FormSliderBar<double>
                        {
                            Caption = nameof(GenerationOptions.HpDrainRate),
                            Current = new BindableDouble(genOptions.HpDrainRate)
                            {
                                MinValue = 0,
                                MaxValue = 10,
                                Precision = 0.1,
                            },
                        },
                    },
                    new Drawable[]
                    {
                        TargetStarRatingNumberBox = new FormSliderBar<double>
                        {
                            Caption = nameof(GenerationOptions.TargetStarRating),
                            Current = new BindableDouble(genOptions.TargetStarRating ?? 5.3)
                            {
                                MinValue = 0.1,
                                MaxValue = 12,
                                Precision = 0.1,
                            },
                        },
                        YearNumberBox = new FormNumberBox
                        {
                            Caption = nameof(GenerationOptions.Year),
                            Current = new Bindable<string>(genOptions.Year.ToString()),
                        },
                        MapperIdNumberBox = new FormNumberBox
                        {
                            Caption = nameof(GenerationOptions.MapperId),
                            Current = new Bindable<string>((genOptions.MapperId ?? 4452992).ToString()),
                        },
                        HitsoundedCheckBox = new FormCheckBox
                        {
                            Caption = nameof(GenerationOptions.Hitsounded),
                            Current = new Bindable<bool>(genOptions.Hitsounded),
                            ExtendedHeight = true
                        },
                    },
                    new Drawable[]
                    {
                        SliderMultiplierTextBox = new FormSliderBar<double>
                        {
                            Caption = nameof(GenerationOptions.SliderMultiplier),
                            Current = new BindableDouble(genOptions.SliderMultiplier)
                            {
                                MinValue = 0.4,
                                MaxValue = 3.6,
                                Precision = 0.01,
                            },
                        },
                        SliderTickRateTextBox = new FormNumberBox
                        {
                            Caption = nameof(GenerationOptions.SliderTickRate),
                            Current = new Bindable<string>(genOptions.SliderTickRate.ToString()),
                        },
                        SeedTextBox = new FormNumberBox
                        {
                            Caption = nameof(GenerationOptions.Seed),
                            Current = new Bindable<string>((genOptions.Seed ?? ((int)new Random().NextInt64())).ToString()),
                        },
                        CfgScaleNumberBox = new FormNumberBox(allowDecimals: true)
                        {
                            Caption = nameof(GenerationOptions.CfgScale),
                            Current = new Bindable<string>(genOptions.CfgScale.ToString(CultureInfo.InvariantCulture)),
                        },
                    },
                    new Drawable[]
                    {
                        DescriptorsDropdown = new LabelledDropdown<string>(true)
                        {
                            Label = new LocalisableString(nameof(GenerationOptions.Descriptors)),
                            RelativeSizeAxes = Axes.X,
                            Items = GenerationOptions.DESCRIPTOR_IDX.Keys,
                            Width = 2f,
                        },
                        Empty(),
                        NegativeDescriptorsDropdown = new LabelledDropdown<string>(true)
                        {
                            Label = new LocalisableString(nameof(GenerationOptions.NegativeDescriptors)),
                            RelativeSizeAxes = Axes.X,
                            Items = GenerationOptions.DESCRIPTOR_IDX.Keys,
                            Width = 2f,
                        },
                    },
            };
            foreach (var textBox in new FormTextBox[]
            {
                    TitleTextBox, ArtistTextBox, CreatorTextBox, DifficultyNameTextBox,
                    YearNumberBox, MapperIdNumberBox,
                    SliderTickRateTextBox, SeedTextBox, CfgScaleNumberBox,
            })
            {
                textBox.TabbableContentContainer = this;
            }
            foreach (var sliderBar in new FormSliderBar<double>[]
            {
                    OverallDifficultyTextBox, ApproachRateTextBox, CircleSizeTextBox, HpDrainRateTextBox,
                    TargetStarRatingNumberBox, SliderMultiplierTextBox
            })
            {
                sliderBar.TabbableContentContainer = this;
            }
        }

        public void ApplyTo(GenerationOptions options)
        {
            options.AudioPath = AudioPathTextBox.Current.Value;
            options.ModelDirectory = ModelDirectoryTextBox.Current.Value;
            options.Title = TitleTextBox.Current.Value;
            options.Artist = ArtistTextBox.Current.Value;
            options.Creator = CreatorTextBox.Current.Value;
            options.DifficultyName = DifficultyNameTextBox.Current.Value;
            options.GameMode = GameModeTextBox.Current.Value;

            options.OverallDifficulty = OverallDifficultyTextBox.Current.Value;
            options.ApproachRate = ApproachRateTextBox.Current.Value;
            options.CircleSize = CircleSizeTextBox.Current.Value;
            options.HpDrainRate = HpDrainRateTextBox.Current.Value;
            options.SliderMultiplier = SliderMultiplierTextBox.Current.Value;
            options.SliderTickRate = parseDouble(SliderTickRateTextBox, options.SliderTickRate);
            options.Seed = parseNullableInt(SeedTextBox);

            options.TargetStarRating = TargetStarRatingNumberBox.Current.Value;
            options.Year = parseInt(YearNumberBox, options.Year);
            options.MapperId = parseNullableInt(MapperIdNumberBox);
            options.Hitsounded = HitsoundedCheckBox.Current.Value;

            if (!string.IsNullOrEmpty(DescriptorsDropdown.Current.Value))
                options.Descriptors.Add(DescriptorsDropdown.Current.Value);//selectedAsList(DescriptorsDropdown.);
            if (!string.IsNullOrEmpty(NegativeDescriptorsDropdown.Current.Value))
                options.NegativeDescriptors.Add(NegativeDescriptorsDropdown.Current.Value);//selectedAsList(NegativeDescriptorsDropdown);

            options.CfgScale = parseDouble(CfgScaleNumberBox, options.CfgScale);
            options.DoSample = DoSampleCheckBox.Current.Value;
            options.CudaDeviceId = parseInt(CudaDeviceIdNumberBox, options.CudaDeviceId);
            options.VerifyDecoderCache = VerifyDecoderCacheCheckBox.Current.Value;
        }

        private static double parseDouble(FormNumberBox box, double fallback)
            => double.TryParse(box.Current.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : fallback;

        private static double? parseNullableDouble(FormNumberBox box)
            => double.TryParse(box.Current.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : null;

        private static int parseInt(FormNumberBox box, int fallback)
            => int.TryParse(box.Current.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

        private static int? parseNullableInt(FormNumberBox box)
            => int.TryParse(box.Current.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : null;

        private static List<string> selectedAsList(Dropdown<string> dropdown)
            => string.IsNullOrEmpty(dropdown.Current.Value) ? new List<string>() : new List<string> { dropdown.Current.Value };
    }
}
