// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes a faction from the target mob.
/// </summary>
public sealed partial class RemoveFaction : EntityEffectBase<RemoveFaction>
{
    /// <summary>
    /// The faction to remove.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed class RemoveFactionEffectSystem : EntityEffectSystem<NpcFactionMemberComponent, RemoveFaction>
{
    [Dependency] private NpcFactionSystem _faction = default!;

    protected override void Effect(Entity<NpcFactionMemberComponent> ent, ref EntityEffectEvent<RemoveFaction> args)
    {
        _faction.RemoveFaction(ent.AsNullable(), args.Effect.Faction);
    }
}
