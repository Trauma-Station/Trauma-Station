using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction.UI;

public sealed partial class ConstructionMenu
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IEntitySystemManager _system = default!;

    private readonly CommonKnowledgeSystem _knowledge = default!;

    // TODO: make this an event a trauma.client system injects with
    public void AddSkillRequirements(ConstructionPrototype proto)
    {
        var same = proto.Practical == null;
        RecipeConstructionList.AddChild(new Label()
        {
            Text = Loc.GetString("construction-menu-requirement-theory", ("same", same))
        });
        AddSkills(proto.Theory);

        if (proto.Practical is {} practical)
        {
            RecipeConstructionList.AddChild(new Label()
            {
                Text = Loc.GetString("construction-menu-requirement-practical")
            });
            AddSkills(practical);
        }
    }

    private void AddSkills(Dictionary<EntProtoId, int> skills)
    {
        if (skills.Count == 0)
        {
            RecipeConstructionList.AddChild(new Label()
            {
                Text = Loc.GetString("construction-menu-requirement-none")
            });
        }

        foreach (var (id, amount) in skills)
        {
            // TODO: use AllSkills
            if (!_proto.Resolve(id, out var prototype) || !prototype.TryGetComponent<KnowledgeComponent>("Knowledge", out var skill))
                continue;

            var text = Loc.GetString("construction-menu-requirement-display",
                ("name", prototype.Name),
                ("amount", _knowledge.GetMasteryString(amount)));
            RecipeConstructionList.AddChild(new Label()
            {
                Text = text,
                Modulate = skill.Color
            });
        }
    }
}
