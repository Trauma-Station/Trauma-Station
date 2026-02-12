// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Skinnable;

[RegisterComponent, NetworkedComponent, Access(typeof(SkinnableSystem))]
[AutoGenerateComponentState]
public sealed partial class SkinnableComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Skinned;

    /// <summary>
    /// Sprite to set every limb's visuals to which previously had <see cref="UnskinnedSprite"/>.
    /// </summary>
    [DataField(required: true)]
    public string SkinnedSprite = string.Empty;

    /// <summary>
    /// The sprite to find limbs which are considered unskinned.
    /// Used to prevent e.g. bionic arms being changed to skinned.
    /// </summary>
    [DataField(required: true)]
    public string UnskinnedSprite = string.Empty;

    [DataField]
    public TimeSpan SkinningDoAfterDuation = TimeSpan.FromSeconds(5);

    [DataField]
    public DamageSpecifier DamageOnSkinned = new() { DamageDict = new Dictionary<string, FixedPoint2> { { "Slash", 50 } } };

    [DataField]
    public SoundSpecifier SkinSound = new SoundPathSpecifier("/Audio/_Shitmed/Medical/Surgery/scalpel1.ogg");
}
