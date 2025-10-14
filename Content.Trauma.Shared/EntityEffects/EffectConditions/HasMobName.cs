using Content.Shared.EntityEffects;
using Content.Shared.IdentityManagement;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects.EffectConditions;

public sealed partial class HasMobName : EntityEffectCondition
{
    [DataField(required: true)]
    public string Name = string.Empty;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        var entName = args.EntityManager.GetComponent<MetaDataComponent>(args.TargetEntity).EntityName;
        return entName.IndexOf(Name, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-has-mob-name", ("name", Name));
    }
}
