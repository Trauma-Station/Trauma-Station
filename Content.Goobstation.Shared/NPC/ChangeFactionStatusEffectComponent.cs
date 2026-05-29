// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.NPC.Prototypes;

namespace Content.Goobstation.Shared.NPC;

[RegisterComponent, NetworkedComponent, Access(typeof(ChangeFactionStatusEffectSystem))]
public sealed partial class ChangeFactionStatusEffectComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype>? NewFaction;

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> OldFactions = new();
}
