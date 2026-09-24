// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a faction to the target mob.
/// </summary>
public sealed partial class AddFaction : EntityEffectBase<AddFaction>
{
    /// <summary>
    /// The faction to add.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class AddFactionEffectSystem : EntityEffectSystem<NpcFactionMemberComponent, AddFaction>
{
    [Dependency] private NpcFactionSystem _faction = default!;

    protected override void Effect(Entity<NpcFactionMemberComponent> ent, ref EntityEffectEvent<AddFaction> args)
    {
        _faction.AddFaction(ent.AsNullable(), args.Effect.Faction);
    }
}
