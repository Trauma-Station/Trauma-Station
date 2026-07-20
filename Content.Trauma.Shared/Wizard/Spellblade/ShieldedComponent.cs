// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Trauma.Shared.Heretic.Components;

namespace Content.Trauma.Shared.Wizard.Spellblade;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShieldedComponent : BaseSpriteOverlayComponent
{
    [DataField]
    public float Lifetime = 5f;

    [DataField]
    public bool AntiStun = true;

    [DataField]
    public DamageModifierSet Resistances = new()
    {
        Coefficients = new()
        {
            ["Blunt"] = 0.5f,
            ["Slash"] = 0.5f,
            ["Piercing"] = 0.5f,
            ["Ballistic"] = 0.5f,
            ["Heat"] = 0.5f
        }
    };

    public override bool Unshaded { get; set; } = false;

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Wizard/Effects/effects.rsi"), "shield-old");

    public override Enum Key { get; set; } = ShieldedKey.Key;
}

public enum ShieldedKey : byte
{
    Key,
}
