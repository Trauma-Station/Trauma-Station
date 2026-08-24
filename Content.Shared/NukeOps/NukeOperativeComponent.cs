using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.NukeOps;

/// <summary>
/// This is used for tagging a mob as a nuke operative.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NukeOperativeComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> SyndStatusIcon = "SyndicateFaction";
}
