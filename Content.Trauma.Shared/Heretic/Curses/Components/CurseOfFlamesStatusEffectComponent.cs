// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Curses.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CurseOfFlamesStatusEffectComponent : Component
{
    [DataField]
    public float MinFireStacks = 2f;

    [DataField]
    public float Penetration = 0.5f;

    [DataField, AutoPausedField]
    public TimeSpan NextIgnition = TimeSpan.Zero;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(10);
}
