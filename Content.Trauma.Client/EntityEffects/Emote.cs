using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.EntityEffects;

// TODO: remove this chud shit when entity effects are moved to shared
public sealed partial class Emote : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}
