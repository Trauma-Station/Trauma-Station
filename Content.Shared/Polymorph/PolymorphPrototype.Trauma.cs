using Content.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Polymorph;

[Serializable, NetSerializable] // some shit needs it i imagine
public sealed partial record PolymorphConfiguration
{
    /// <summary>
    /// If <see cref="Entity"/> is null, entity will be picked from this weighted random.
    /// Doesn't support polymorph actions.
    /// </summary>
    [DataField(serverOnly: true)]
    public ProtoId<WeightedRandomEntityPrototype>? Entities;

    /// <summary>
    /// If <see cref="Entity"/> and <see cref="Entities"/>> is null,
    /// weighted entity random will be picked from this weighted random.
    /// Doesn't support polymorph actions.
    /// </summary>
    [DataField(serverOnly: true)]
    public ProtoId<WeightedRandomPrototype>? Groups;

    /// <summary>
    /// Transfers these components on polymorph.
    /// Does nothing on revert.
    /// </summary>
    [DataField(serverOnly: true)]
    public HashSet<ComponentTransferData> ComponentsToTransfer = new()
    {
        new("LanguageKnowledge"),
        new("LanguageSpeaker"),
        new("Grammar"),
    };

    /// <summary>
    /// Whether polymorphed entity should be able to move.
    /// </summary>
    [DataField]
    public bool AllowMovement = true;

    /// <summary>
    /// Whether to show popup on polymorph revert.
    /// </summary>
    [DataField]
    public bool ShowPopup = true;

    /// <summary>
    /// Whether to insert polymorphed entity into container or attach to grid or map.
    /// </summary>
    [DataField]
    public bool AttachToGridOrMap;

    /// <summary>
    /// Skip revert action confirmation
    /// </summary>
    [DataField]
    public bool SkipRevertConfirmation;

    /// <summary>
    /// Whether to strip name modifier if transferring name.
    /// Can be disabled if you want the modifier suffix to be transferred.
    /// </summary>
    [DataField]
    public bool StripNameModifier = true;

    /// <summary>
    /// Lets you disable making the new entity sentient, for non-mob polymorphs.
    /// </summary>
    [DataField]
    public bool MakeSentient = true;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ComponentTransferData(string component, bool @override = true, bool mirror = false)
{
    [DataField(required: true)]
    public string Component = component;

    [DataField]
    public bool Override = @override;

    /// <summary>
    /// Whether we should copy the component data if false or just ensure it on a new entity if true
    /// </summary>
    [DataField]
    public bool Mirror = mirror;

    public ComponentTransferData() : this(string.Empty, true, false) { }
}
