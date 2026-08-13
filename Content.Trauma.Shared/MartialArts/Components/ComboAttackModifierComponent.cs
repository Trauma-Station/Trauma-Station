// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.MartialArts;

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Applies martial art modifiers every time an attack lands, before any combo is checked.
/// Requires <see cref="MartialArtModifiersComponent"/>.
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
    /// <summary>
    /// Only apply for this kind of attack, or every attack if null.
    /// </summary>
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

    /// <summary>
    /// If set, the multiplier becomes the user's velocity raised to this power, clamped between
    /// <see cref="MinMultiplier"/> and <see cref="MaxMultiplier"/>. Standing still gives no bonus.
    /// </summary>
    [DataField]
    public float? VelocityExponent;

    [DataField]
    public float MinMultiplier = 1f;

    [DataField]
    public float MaxMultiplier = 1.5f;
}
