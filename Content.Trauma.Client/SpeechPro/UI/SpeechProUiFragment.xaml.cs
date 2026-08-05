// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Shared.SpeechPro;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.SpeechPro.UI;

[GenerateTypedNameReferences]
public sealed partial class SpeechProUiFragment : BoxContainer
{
    private const float TwoColumnWidth = 360f;
    private const float FourColumnWidth = 560f;
    private int _columns = 3;

    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly Dictionary<ProtoId<SpeechProPhraseGroupPrototype>, GridContainer> _grids = new();

    public event Action<ProtoId<SpeechProPhrasePrototype>>? OnPhraseSelected;

    public SpeechProUiFragment()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        PopulateGroups();
    }

    private void PopulateGroups()
    {
        foreach (var group in _prototype.EnumeratePrototypes<SpeechProPhraseGroupPrototype>().OrderBy(group => group.Order))
        {
            GroupsContainer.AddChild(new Label
            {
                Text = Loc.GetString(group.Name),
                StyleClasses = { "LabelSubText" },
                Margin = new Thickness(2, _grids.Count == 0 ? 0 : 5, 2, 1),
            });

            var grid = new GridContainer
            {
                Columns = _columns,
                HorizontalExpand = true,
            };

            _grids.Add(group.ID, grid);
            GroupsContainer.AddChild(grid);

            foreach (var phraseId in group.Phrases)
            {
                if (!_prototype.Resolve(phraseId, out var phrase))
                    continue;

                AddButton(grid, phrase);
            }
        }
    }

    private void AddButton(GridContainer grid, SpeechProPhrasePrototype phrase)
    {
        var button = new Button
        {
            Text = Loc.GetString(phrase.Button),
            ClipText = true,
            HorizontalExpand = true,
            Margin = new Thickness(1),
            MinHeight = 28,
        };

        button.OnPressed += _ => OnPhraseSelected?.Invoke(phrase.ID);
        grid.AddChild(button);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var columns = Size.X switch
        {
            < TwoColumnWidth => 2,
            >= FourColumnWidth => 4,
            _ => 3,
        };

        if (_columns == columns)
            return;

        _columns = columns;
        foreach (var grid in _grids.Values)
        {
            grid.Columns = columns;
        }
    }
}
