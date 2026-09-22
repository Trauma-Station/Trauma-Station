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
    /// The rune entity to spawn when fully drawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// The unfinished blood ring to spawn first.
    /// </summary>
    [DataField]
    public EntProtoId Unfinished = "CultRuneUnfinishedRegular";

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

    /// <summary>
    /// Whether to require placement in and consume one of the randomly picked areas.
    /// </summary>
    [DataField]
    public bool AreaLimited;

    /// <summary>
    /// Whether to require sacrificing the cult's target before placing.
    /// </summary>
    [DataField]
    public bool RequireTarget;

    /// <summary>
    /// If nonzero, the maximum number of this rune a cult can have.
    /// </summary>
    [DataField]
    public int Limit;

    /// <summary>
    /// Announcement to make from central command after starting to draw this rune.
    /// Gets "area" passed with the name of the area it was placed in.
    /// </summary>
    [DataField]
    public LocId? Announcement;
}
