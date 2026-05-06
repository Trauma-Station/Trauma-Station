using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.MobClass;

/// <summary>
/// This prototype groups together <see cref="MobClassPrototype"/>.
/// Useful for defining which classes a mob can specialize with.
/// </summary>
[Prototype]
public sealed partial class MobClassGroupPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The list of classes belonging to this group.
    /// </summary>
    [DataField]
    public List<ProtoId<MobClassPrototype>> Classes = new();
}
