// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Genetics.Console;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Client.Genetics.UI;

[GenerateTypedNameReferences]
public sealed partial class GeneticStorage : BoxContainer
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public event Action<uint>? OnPrint;

    private GeneticsConsoleComponent _comp = default!;
    private CompName _costName;
    private EntProtoId<MutationComponent>? _diskMutation;
    private bool _cooldown;
    private List<Button> _buttons = new();

    public GeneticStorage()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _costName = _ent.ComponentFactory.CompName<MutationCostComponent>();
    }

    public void SetConsole(GeneticsConsoleComponent comp)
    {
        _comp = comp;
        RebuildPrintButtons();
    }

    private int GetCostMultiplier()
    {
        if (_diskMutation is not { } id ||
            !_proto.Resolve(id, out var proto) ||
            !proto.TryComp<MutationCostComponent>(_costName, out var cost))
            return 1;

        return cost.BiomassMultiplier;
    }

    private void RebuildPrintButtons()
    {
        _buttons.Clear();
        PrintButtons.RemoveAllChildren();
        var multiplier = GetCostMultiplier();
        var disabled = _cooldown || _diskMutation == null;
        for (var i = 0; i < _comp.Prints.Count; i++)
        {
            var index = (uint) i;
            var cost = _comp.Prints[i].Cost * multiplier;
            var proto = _comp.Prints[i].Proto;
            var button = new Button()
            {
                Text = Loc.GetString("genetics-console-print-item", ("item", _proto.Index(proto).Name), ("cost", cost)),
                MaxWidth = 160f,
                Margin = new Thickness(4),
                HorizontalExpand = true
            };
            button.Disabled = disabled;
            button.OnPressed += _ => OnPrint?.Invoke(index);
            _buttons.Add(button);
            PrintButtons.AddChild(button);
        }
    }

    public void UpdateDisk(Entity<GeneticsDiskComponent>? disk)
    {
        DiskName.Text = _ent.GetComponentOrNull<MetaDataComponent>(disk)?.EntityName
            ?? Loc.GetString("genetics-console-no-disk");
    }

    public void UpdateDiskMutation(EntProtoId<MutationComponent>? mutation)
    {
        _diskMutation = mutation;
        MutationName.Text = _proto.TryIndex(mutation, out var proto)
            ? proto.Name
            : Loc.GetString("genetics-console-disk-empty");
        UpdateCooldowns(_cooldown);
        RebuildPrintButtons();
    }

    public void UpdateCooldowns(bool cooldown)
    {
        _cooldown = cooldown;
        var disabled = cooldown || _diskMutation == null;
        foreach (var button in _buttons)
        {
            button.Disabled = disabled;
        }
    }
}
