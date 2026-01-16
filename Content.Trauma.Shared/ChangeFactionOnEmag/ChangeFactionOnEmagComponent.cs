using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.ChangeFactionOnEmag;

/// <summary>
/// Changes A entities faction when it gets emaged
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangeFactionOnEmagComponent : Component
{
    [DataField(required: true)]
    public ProtoId<NpcFactionPrototype> Faction;
}
