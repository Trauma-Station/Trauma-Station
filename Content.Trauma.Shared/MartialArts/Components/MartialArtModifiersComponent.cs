// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Stores temporary combat modifiers granted by a martial art, each expiring on its own timer.
/// This goes on the knowledge entity and not the mob, so <see cref="User"/> is who actually gets buffed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class MartialArtModifiersComponent : Component
{
    /// <summary>
    /// Every modifier currently running.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<MartialArtModifierData> Data = new();

    /// <summary>
    /// The mob these modifiers apply to, set when the first one is added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// Damage type used by flat <see cref="MartialArtModifierType.Damage"/> modifiers.
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype> FlatDamageType = "Blunt";

    /// <summary>
    /// When the soonest modifier expires, so idle arts aren't checked every tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.MaxValue;

    /// <summary>
    /// Clamps applied to the total of every modifier of a type, stopping combos from stacking into absurdity.
    /// </summary>
    [DataField]
    public Dictionary<MartialArtModifierType, MartialArtModifierLimit> Limits = new()
    {
        [MartialArtModifierType.AttackRate] = new() { MaxMultiplier = 4f, MinModifier = -4f, MaxModifier = 4f },
        [MartialArtModifierType.Damage] = new() { MaxMultiplier = 3f, MinModifier = -20f, MaxModifier = 20f },
        [MartialArtModifierType.MoveSpeed] = new() { MinMultiplier = 0.2f, MaxMultiplier = 1.5f },
    };
}

/// <summary>
/// A single temporary modifier and when it runs out.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public partial record struct MartialArtModifierData
{
    /// <summary>
    /// What this modifies, and when it is allowed to apply.
    /// </summary>
    public MartialArtModifierType Type = MartialArtModifierType.AttackRate;

    /// <summary>
    /// Multiplied with every other multiplier of the same type.
    /// </summary>
    public float Multiplier = 1f;

    /// <summary>
    /// Added to every other flat modifier of the same type.
    /// </summary>
    public float Modifier;

    /// <summary>
    /// When this gets removed.
    /// </summary>
    public TimeSpan EndTime;

    public MartialArtModifierData()
    {
    }

    /// <summary>
    /// Whether this modifier has a given flag set.
    /// </summary>
    public readonly bool Has(MartialArtModifierType flag) => (Type & flag) != 0;
}

/// <summary>
/// Bounds for the combined multiplier and flat modifier of a single <see cref="MartialArtModifierType"/>.
/// </summary>
[DataRecord]
public partial record struct MartialArtModifierLimit
{
    public float MinMultiplier = 0.5f;
    public float MaxMultiplier = 4f;
    public float MinModifier;
    public float MaxModifier;

    public MartialArtModifierLimit()
    {
    }
}

[Flags, Serializable, NetSerializable]
public enum MartialArtModifierType : byte
{
    None = 0,

    /// <summary>
    /// How fast you swing.
    /// </summary>
    AttackRate = 1 << 0,

    /// <summary>
    /// How hard you hit.
    /// </summary>
    Damage = 1 << 1,

    /// <summary>
    /// How fast you move.
    /// </summary>
    MoveSpeed = 1 << 2,

    /// <summary>
    /// Only apply when attacking with bare hands.
    /// </summary>
    Unarmed = 1 << 3,

    /// <summary>
    /// Only apply when attacking with a weapon.
    /// </summary>
    Armed = 1 << 4,
}
