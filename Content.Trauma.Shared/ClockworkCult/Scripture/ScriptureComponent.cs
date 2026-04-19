// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.ClockworkCult.Scripture;

/// <summary>
/// Attach this to anything that you want to appear in the Clockwork Slab
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ScriptureSystem)), AutoGenerateComponentState]
public sealed partial class ScriptureComponent : Component
{
    /// <summary>
    /// The power it costs to cast this scripture, measured in Watts
    /// </summary>
    [DataField]
    public int PowerCost = 1000;

    /// <summary>
    /// Effects to run on recital.
    /// E.g. giving an action to the user, or spawning a structure
    /// </summary>
    [DataField]
    public EntityEffect[]? RecitalEffects;

    /// <summary>
    /// How many seconds the user has to wait before reciting this scripture again.
    /// Used to prevent spam.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The last recital attempt that was commited. Used along <see cref="Delay"/>.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan LastTry;
};
