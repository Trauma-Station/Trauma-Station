// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;

namespace Content.Trauma.Shared.MartialArts.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class MartialArtModifiersComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<MartialArtModifierData> Data = new();

    [DataField, AutoNetworkedField]
    public EntityUid? User;

    [DataField]
    public ProtoId<DamageTypePrototype> FlatDamageType = "Blunt";

    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.MaxValue;

    [DataField]
    public Dictionary<MartialArtModifierType, MartialArtModifierLimit> Limits = new()
    {
        [MartialArtModifierType.AttackRate] = new() { MaxMultiplier = 4f, MinModifier = -4f, MaxModifier = 4f },
        [MartialArtModifierType.Damage] = new() { MaxMultiplier = 3f, MinModifier = -20f, MaxModifier = 20f },
        [MartialArtModifierType.MoveSpeed] = new() { MinMultiplier = 0.2f, MaxMultiplier = 1.5f },
    };
}

[DataRecord, Serializable, NetSerializable]
public sealed partial class MartialArtModifierData
{
    public MartialArtModifierType Type = MartialArtModifierType.AttackRate;

    public float Multiplier = 1f;

    public float Modifier;

    public TimeSpan EndTime;
}

[DataDefinition]
public sealed partial class MartialArtModifierLimit
{
    [DataField]
    public float MinMultiplier = 0.5f;

    [DataField]
    public float MaxMultiplier = 4f;

    [DataField]
    public float MinModifier;

    [DataField]
    public float MaxModifier;
}

[Flags, Serializable, NetSerializable]
public enum MartialArtModifierType : byte
{
    None = 0,
    AttackRate = 1 << 0,
    Damage = 1 << 1,
    MoveSpeed = 1 << 2,
    Unarmed = 1 << 3,
    Armed = 1 << 4,
}
