// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.GameTicking.Rules.Components;

[RegisterComponent]
public sealed partial class NewAntagOrEvacComponent : Component
{
    /// <summary>
    /// How many alive players there were when this game-rule started.
    /// </summary>
    [DataField]
    public int PlayersOnStart;

    /// <summary>
    /// The percent required for a new antag to be spawned.
    /// </summary>
    [DataField]
    public float Percent = 0.6f;

    /// <summary>
    /// The event to start.
    /// </summary>
    [DataField]
    public EntProtoId Event = "ModerateAntagEventScheduler";
}
