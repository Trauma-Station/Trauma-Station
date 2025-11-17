using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Gibbing.Events;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Gibs the target mob.
/// </summary>
public sealed partial class Gib : EntityEffectBase<Gib>
{
    [DataField]
    public bool GibOrgans;

    [DataField]
    public bool LaunchGibs = true;

    [DataField]
    public GibType GibType = GibType.Gib;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-gib", ("chance", Probability));
}

public sealed class GibEffectSystem : EntityEffectSystem<BodyComponent, Gib>
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<Gib> args)
    {
        var effect = args.Effect;
        _body.GibBody(ent,
            effect.GibOrgans,
            ent.Comp,
            effect.LaunchGibs,
            gib: effect.GibType);
    }
}
