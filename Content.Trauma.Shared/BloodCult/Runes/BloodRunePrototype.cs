// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.BloodCult.Runes;

/// <summary>
/// A blood cult rune that can be drawn using a cult dagger with <see cref="RuneDrawerComponent"/>.
/// </summary>
[Prototype]
public sealed partial class BloodRunePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The rune entity to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// How long it takes to draw the rune.
    /// </summary>
    [DataField]
    public TimeSpan DrawTime = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Damage dealt to the user after drawing the rune.
    /// </summary>
    [DataField]
    public DamageSpecifier DrawDamage = new()
    {
        DamageDict = new()
        {
            ["Slash"] = 15,
        }
    };
}
