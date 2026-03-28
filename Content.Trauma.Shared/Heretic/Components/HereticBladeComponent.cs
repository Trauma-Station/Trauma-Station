// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticBladeComponent : Component
{
    [DataField]
    public HereticPath? Path;

    [DataField]
    public EntityEffect[]? Effects;

    [DataField, NonSerialized]
    public HereticBladeBonusEvent? BonusEvent;

    /// <summary>
    /// Path stage -> effect probability
    /// </summary>
    [DataField]
    public Dictionary<int, float> Probabilities = new()
    {
        { 0, 1f },
    };

    [DataField]
    public SoundSpecifier? ShatterSound = new SoundCollectionSpecifier("GlassBreak");

    [DataField]
    public SoundSpecifier ArrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    [DataField]
    public SoundSpecifier DepartureSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}
