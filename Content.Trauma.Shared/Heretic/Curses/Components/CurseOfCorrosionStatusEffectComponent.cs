// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Damage;

namespace Content.Trauma.Shared.Heretic.Curses.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CurseOfCorrosionStatusEffectComponent : Component
{
    [DataField, AutoPausedField]
    public TimeSpan NextVomit = TimeSpan.Zero;

    [DataField]
    public Vector2 MinMaxSecondsBetweenVomits = new(5f, 20f);

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict =
        {
            { "Poison", 0.3f },
        },
    };
}
