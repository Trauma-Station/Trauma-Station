// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlowdownResistanceComponent : Component
{
    /// <summary>
    /// Flat reduction to user's movespeed modifier when added to clothing or entity itself
    /// </summary>
    [DataField]
    public float Reduction = 0.2f;

    /// <summary>
    /// Will this only increase speed for the entity itself or be "globally" speed up the user
    /// For example, if applied to boots, if Global = true it will affect all speed modifiers on user,
    /// if Global = false it will only affect slowdown tied to these boots
    /// </summary>
    [DataField]
    public bool Global = true;
}
