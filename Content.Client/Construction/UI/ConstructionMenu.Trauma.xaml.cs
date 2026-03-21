using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction.UI;

public sealed partial class ConstructionMenu
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntitySystemManager _system = default!;

    private readonly CommonKnowledgeSystem _knowledge = default!;

    public void AddSkillRequirements(ConstructionPrototype proto)
    {
        if (proto.Practical is not { })
        {
            RecipeConstructionList.AddItem(Loc.GetString("construction-menu-requirement-none"));
            return;
        }

        foreach (var (id, amount) in proto.Practical)
        {
            if (!_proto.Resolve(id, out var prototype) || !prototype.Components.ContainsKey("Knowledge"))
                continue;

            var skill = (KnowledgeComponent) prototype.Components["Knowledge"].Component;
            var text = Loc.GetString("construction-menu-requirement-display", ("name", prototype.Name), ("amount", _knowledge.GetMasteryString(amount)));
            RecipeConstructionList.AddItem($"[color={skill.Color}]{text}[/color]");

        }
    }
}
