using Content.Shared.Body;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Autosurgeon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoSurgeonComponent : Component
{
    [DataField(required: true)]
    public List<AutoSurgeonEntry> Entries = new();

    [DataField]
    public bool OneTimeUse = true;

    [DataField, AutoNetworkedField]
    public bool Used;

    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(15);

    [DataField] // If you're changing this, do not forget the loop
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_Goobstation/Machines/autosurgeon.ogg", AudioParams.Default.WithLoop(true));

    [DataField]
    public EntityUid? ActiveSound;
}

[DataDefinition]
public sealed partial class AutoSurgeonEntry
{
    /// <summary>
    /// The target organ category to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> TargetCategory;

    /// <summary>
    /// These components will be added to the part itself, for example, MantisBladeArm.
    /// If this is not null but the targeted part has all of them already, the surgery would fail.
    /// This is only applied if NewPartProto is null, so we are upgrading and not replacing a part.
    /// </summary>
    [DataField]
    public ComponentRegistry? OrganComponents;

    /// <summary>
    /// These components will be added to organ as OrganComponentsComponent.OnAdd and applied to the user, for example, LeftMantisBladeUser.
    /// This is only applied if NewOrganProto is null, so we are upgrading and not replacing a part.
    /// </summary>
    [DataField]
    public ComponentRegistry? UserComponents;

    /// <summary>
    /// If it's null, will upgrade the body part/organ. If not, will replace it with this proto.
    /// </summary>
    [DataField]
    public EntProtoId<OrganComponent>? NewOrganProto;
}
