// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._Goobstation.Weapons.AmmoSelector;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Weapons.Ranged;

/// <summary>
/// Provides magazine visuals for different ammo selector settings.
/// </summary>
[RegisterComponent]
public sealed partial class AmmoSelectorMagazineVisualsComponent : Component
{
    /// <summary>
    /// What RsiState we use for each selectable ammo prototype.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<SelectableAmmoPrototype>, string> MagStates = new();

    /// <summary>
    /// How many steps there are
    /// </summary>
    [DataField(required: true)]
    public int MagSteps;

    /// <summary>
    /// Should we hide when the count is 0
    /// </summary>
    [DataField]
    public bool ZeroVisible;
}
