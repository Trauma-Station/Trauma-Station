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

/// <summary>
/// A single rule for when to apply a modifier and how strong it is.
/// </summary>
[DataRecord]
public sealed partial class ComboAttackModifier
{
    /// <summary>
    /// Attack types that trigger this, null for any of them.
    /// </summary>
    public List<ComboAttackType>? AttackTypes;

    /// <summary>
    /// Whether this only triggers when attacking with bare hands.
    /// </summary>
    public bool UnarmedOnly;

    /// <summary>
    /// What the applied modifier changes.
    /// </summary>
    public MartialArtModifierType Type = MartialArtModifierType.AttackRate;

    /// <summary>
    /// Flat multiplier to apply, ignored when <see cref="VelocityExponent"/> is set.
    /// </summary>
    public float Multiplier = 1f;

    public float Modifier;

    public TimeSpan Duration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// If set, the multiplier is the user's velocity raised to this instead of <see cref="Multiplier"/>.
    /// </summary>
    public float? VelocityExponent;

    /// <summary>
    /// Bounds for the velocity scaled multiplier.
    /// </summary>
    public float MinMultiplier = 1f;
    public float MaxMultiplier = 1.5f;
}
