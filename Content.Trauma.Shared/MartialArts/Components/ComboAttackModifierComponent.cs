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
    /// Event raised on the performer to calculate the multiplier.
    /// </summary>
    public BaseComboMultiplierEvent Multiplier = new FlatMultiplierEvent();

    public float Modifier;

    public TimeSpan Duration = TimeSpan.FromSeconds(3);
}
