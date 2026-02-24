using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;

namespace Content.Shared._DV.CosmicCult.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CosmicLesserCultistComponent : Component
{
    /// <summary>
    /// The status icon prototype displayed for cosmic cultists.
    /// </summary>
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "CosmicCultLesserIcon";

    /// <summary>
    /// Whether or not this cultist was weak to holy before conversion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WasWeakToHoly;

    /// <summary>
    /// A string for storing what damage container this cultist had upon conversion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DamageContainerPrototype> StoredDamageContainer = "Biological";

    [DataField]
    public EntProtoId DamageTransferAction = "ActionCosmicDamageTransfer";

    public EntityUid? DamageTransferActionEntity;

    [DataField]
    public EntProtoId TransferVFX = "CosmicGenericVFX";

    [DataField]
    public SoundSpecifier TransferSFX = new SoundPathSpecifier("/Audio/_DV/CosmicCult/glyph_trigger.ogg");
}
