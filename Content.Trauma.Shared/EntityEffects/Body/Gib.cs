// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Gibbing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Gibs the target mob or gibbable (body part).
/// </summary>
public sealed partial class Gib : EntityEffectBase<Gib>
{
    [DataField]
    public bool DropGiblets = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-gib", ("chance", Probability));
}

public sealed class GibEffectSystem : EntityEffectSystem<TransformComponent, Gib>
{
    [Dependency] private readonly EffectDataSystem _data = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Gib> args)
    {
        var dropGiblets = args.Effect.DropGiblets;
        var user = _data.GetUser(ent);
        _gibbing.Gib(ent, dropGiblets, user);
    }
}
