using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Virology;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DiseasePenComponent : Component
{
    [DataField]
    public int? Genotype;

    [DataField, AutoNetworkedField]
    public EntityUid? DiseaseUid;

    [DataField]
    public bool Used;

    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    [DataField]
    public bool Vaccine = true;

    [DataField]
    public TimeSpan InjectTime = TimeSpan.FromSeconds(8);
};
