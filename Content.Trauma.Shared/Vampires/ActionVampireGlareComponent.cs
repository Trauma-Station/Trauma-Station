// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Vampires;

/// <summary>
/// <inheritdoc cref="VampireGlareSystem"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActionVampireGlareComponent : Component
{
    /// <summary>
    /// The range of the AoE.
    /// </summary>
    [DataField]
    public float Range = 1f;

    /// <summary>
    /// Effects applied to those from behind.
    /// </summary>
    [DataField]
    public EntityEffect[] BehindEffects = default!;

    /// <summary>
    /// Effects applied to those in front.
    /// </summary>
    [DataField]
    public EntityEffect[] FrontEffects = default!;

    /// <summary>
    /// Effects applied to those on the sides.
    /// </summary>
    [DataField]
    public EntityEffect[] SideEffects = default!;
}

public sealed partial class VampireGlareEvent : InstantActionEvent;
