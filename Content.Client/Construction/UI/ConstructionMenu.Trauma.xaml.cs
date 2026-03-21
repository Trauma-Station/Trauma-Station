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
        var skills = proto.Practical ?? proto.Theory;
        if (skills.Count <= 0)
        {
            var text = Loc.GetString("construction-menu-requirement-none");
            var label = new RichTextLabel
            {
                Text = text,
            };
            RecipeConstructionList.AddChild(label);
            return;
        }

        foreach (var (id, amount) in skills)
        {
            if (!_proto.Resolve(id, out var prototype) || !prototype.TryGetComponent<KnowledgeComponent>("Knowledge", out var skill))
                continue;

            var text = Loc.GetString("construction-menu-requirement-display", ("name", prototype.Name), ("amount", _knowledge.GetMasteryString(amount)));
            var label = new RichTextLabel
            {
                Text = text,
                Modulate = skill.Color
            };
            RecipeConstructionList.AddChild(label);
        }
    }
}
