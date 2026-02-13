using Content.Medical.Common.Body;
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TourniquetableComponent : Component
{
    [DataField]
    public EntityUid? CurrentTourniquetEntity;

    [DataField, AutoNetworkedField]
    public BodyPartSymmetry SeveredSymmetry;

    [DataField, AutoNetworkedField]
    public BodyPartType SeveredPartType;
}
