using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction.UI;

public sealed partial class ConstructionMenu
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly CommonKnowledgeSystem _knowledge = default!;

    public void AddSkillRequirements(ConstructionPrototype proto)
    {
        if (proto.Practical is not { })
        {
            RecipeConstructionList.AddItem(Loc.GetString("construction-menu-requirement-none"));
            return;
        }

        string? foundString = null;
        int foundInt = 0;
        foreach (var (id, amount) in proto.Practical)
        {
            if (foundString is not { })
            {
                if (!_proto.Resolve(id, out var prototype) || !prototype.Components.ContainsKey("Knowledge"))
                    continue;

                var skill = (KnowledgeComponent) prototype.Components["Knowledge"].Component;

                if (skill.Category != "Crafting")
                    continue;

                foundString = prototype.Name;
                foundInt = amount;
                break;
            }
        }

        RecipeConstructionList.AddItem(Loc.GetString("construction-menu-requirement-main-skill", ("name", foundString ?? "Fabrication"), ("amount", _knowledge.GetMasteryString(foundInt))));

        if (proto.Practical.Count > ((foundString is { }) ? 1 : 0))
            RecipeConstructionList.AddItem(Loc.GetString("construction-menu-requirement-extra-skill"));
        foreach (var (id, amount) in proto.Practical)
        {
            if (!_proto.Resolve(id, out var prototype))
                continue;

            string name = prototype.Name;

            if ((foundString is { } && name == foundString))
                continue;

            RecipeConstructionList.AddItem(Loc.GetString("construction-menu-requirement-display", ("name", name), ("amount", _knowledge.GetMasteryString(amount))));

        }
    }
}
