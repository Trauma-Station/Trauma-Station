// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Medical.Shared.DelayedDeath;

// TODO SHITMED: kill this dogshit and have actual vital organ simulation
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class DelayedDeathComponent : Component
{
    /// <summary>
    /// How long it takes to kill the entity.
    /// </summary>
    [DataField]
    public TimeSpan DeathDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// If true, will prevent *almost* all types of revival.
    /// Right now, this just means it won't allow devils to revive.
    /// </summary>
    [DataField]
    public bool PreventAllRevives;

    /// <summary>
    /// How long it has been since the delayed death timer started.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextDeath;

    /// <summary>
    /// What message is displayed when the time runs out - Goobstation
    /// </summary>
    [DataField]
    public LocId DeathMessageId;

    /// <summary>
    /// What the defib displays when attempting to revive this entity. - Goobstation
    /// </summary>
    [DataField]
    public LocId DefibFailMessageId = "defibrillator-missing-organs";
}
