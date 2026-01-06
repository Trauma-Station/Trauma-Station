using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.NPC;

[RegisterComponent, NetworkedComponent, Access(typeof(ChangeFactionStatusEffectSystem))]
public sealed partial class ChangeFactionStatusEffectComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype>? NewFaction;

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> OldFactions = new();
}
