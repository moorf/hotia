// Copyright (c) moorf 2026. Licensed under the GNU General Public License v3.0.
// See the LICENCE.GPL3 file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;

public partial class FormMultiSelect : FillFlowContainer
{
    private readonly FillFlowContainer<FormCheckBox> list;
    private readonly OsuTextBox searchBox;
    private readonly Container body;

    public IEnumerable<string> Selected =>
        list.Children.Where(c => c.Current.Value).Select(c => c.Caption.ToString());

    public FormMultiSelect(string caption, IEnumerable<string> options)
    {
        RelativeSizeAxes = Axes.X;
        AutoSizeAxes = Axes.Y;
        Direction = FillDirection.Vertical;

        RoundedButton toggle;

        InternalChildren = new Drawable[]
        {
            toggle = new RoundedButton
            {
                RelativeSizeAxes = Axes.X,
                Height = 40,
                Text = caption,
            },
            body = new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 240,
                Alpha = 0, // collapsed by default
                Children = new Drawable[]
                {
                    searchBox = new OsuTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 30,
                        PlaceholderText = "type to search",
                    },
                    new OsuScrollContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        Padding = new MarginPadding { Top = 30 },
                        Height = 240,
                        Child = list = new FillFlowContainer<FormCheckBox>
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            ChildrenEnumerable = options.Select(o => new FormCheckBox
                            {
                                Caption = o,
                                Current = new Bindable<bool>(),
                            }),
                        },
                    },
                },
            },
        };

        searchBox.Current.BindValueChanged(e => applyFilter(e.NewValue), true);

        toggle.Action = () =>
        {
            bool opening = body.Alpha == 0;
            body.Alpha = opening ? 1 : 0;

            if (opening)
                Schedule(() => GetContainingFocusManager()?.ChangeFocus(searchBox));
            else
                searchBox.Current.Value = string.Empty;
        };
    }

    private void applyFilter(string term)
    {
        foreach (var box in list.Children)
        {
            bool match = string.IsNullOrWhiteSpace(term)
                         || box.Caption.ToString().Contains(term.Trim(), StringComparison.OrdinalIgnoreCase);
            box.Alpha = match ? 1 : 0;
        }
    }
}
