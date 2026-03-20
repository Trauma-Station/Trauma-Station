using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction.UI;

public sealed partial class ConstructionMenu
{


    public void AddSkillRequirements(ConstructionPrototype proto, IPrototypeManager protoMan, CommonKnowledgeSystem knowledge)
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
                if (!protoMan.TryIndex<EntityPrototype>(id, out var prototype) || !prototype.Components.ContainsKey("Knowledge"))
                    continue;

                var skill = (KnowledgeComponent) prototype.Components["Knowledge"].Component;

                if (skill.Category != "Crafting")
                    continue;

                foundString = prototype.Name;
                foundInt = amount;
                break;
            }
        }

        RecipeConstructionList.AddItem(Loc.GetString("construction-menu-requirement-main-skill", ("name", foundString ?? "Fabrication"), ("amount", knowledge.GetMasteryString(foundInt))));

        if (proto.Practical.Count > ((foundString is { }) ? 1 : 0))
            RecipeConstructionList.AddItem(Loc.GetString("construction-menu-requirement-extra-skill"));
        foreach (var (id, amount) in proto.Practical)
        {
            if (!protoMan.TryIndex<EntityPrototype>(id, out var prototype))
                continue;

            string name = prototype.Name;

            if ((foundString is { } && name == foundString))
                continue;

            RecipeConstructionList.AddItem(Loc.GetString("construction-menu-requirement-display", ("name", name), ("amount", knowledge.GetMasteryString(amount))));

        }
    }
}
