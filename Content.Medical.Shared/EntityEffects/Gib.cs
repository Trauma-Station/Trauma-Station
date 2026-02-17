// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.Gibbing;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Gibs the target mob or other (organs bodyparts etc)
/// Whatever it is will be deleted afterwards, be careful.
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
    [Dependency] private readonly GibbingSystem _gibbing = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Gib> args)
    {
        var dropGiblets = args.Effect.DropGiblets;
        // TODO SHITMED: pass user from actual sane method
        _gibbing.Gib(ent, dropGiblets);
    }
}
