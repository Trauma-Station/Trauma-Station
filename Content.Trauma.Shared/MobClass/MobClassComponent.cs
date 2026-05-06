namespace Content.Trauma.Shared.MobClass;

/// <summary>
/// Component that stores data related to "classes".
///
/// A class is defined as a form specialization this mob resides in.
/// For example, Vampires have 4 specializations.
///
/// This component can be re-used for other mobs that need it in the future (e.g. Darkspawn).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MobClassComponent : Component
{
    /// <summary>
    /// The class we belong to.
    /// </summary>
    [DataField]
    public ProtoId<MobClassPrototype>? CurrentClass;

    /// <summary>
    /// Which classes we are allowed to take.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MobClassGroupPrototype> BelongsTo;
}
