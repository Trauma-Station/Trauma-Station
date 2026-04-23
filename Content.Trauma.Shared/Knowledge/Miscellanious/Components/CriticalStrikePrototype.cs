using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Knowledge.Miscellanious.Components;

/// <summary>
/// Prototypes for critical hits
/// </summary>
[Prototype]
public sealed partial class CriticalStrikePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Maps critical entries to thresholds.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, List<CriticalEntry>> Entries = new();
}

[DataDefinition]
public sealed partial class CriticalEntry
{
    [DataField(required: true)]
    public int MinThreshold;

    [DataField(required: true)]
    public EntityEffect[] Effects;
}

/// <summary>
/// Prototypes for critical hits
/// </summary>
[Prototype]
public sealed partial class FumblePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Maps fumbles to thresholds.
    /// </summary>
    [DataField]
    public List<CriticalEntry> Entries = new();
}
