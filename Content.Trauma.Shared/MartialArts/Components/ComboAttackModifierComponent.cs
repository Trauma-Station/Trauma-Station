// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.MartialArts;

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Applies martial art modifiers every time an attack lands.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ComboAttackModifierComponent : Component
{
    [DataField]
    public List<ComboAttackModifier> Modifiers = new();
}

[DataDefinition]
public sealed partial class ComboAttackModifier
{
    [DataField]
    public ComboAttackType? AttackType;

    [DataField]
    public MartialArtModifierType Type = MartialArtModifierType.AttackRate;

    [DataField]
    public float Multiplier = 1f;

    [DataField]
    public float Modifier;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3);

    [DataField]
    public float? VelocityExponent;

    [DataField]
    public float MinMultiplier = 1f;

    [DataField]
    public float MaxMultiplier = 1.5f;
}
