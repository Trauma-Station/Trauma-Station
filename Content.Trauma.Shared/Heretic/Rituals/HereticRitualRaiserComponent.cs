// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Rituals;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticRitualRaiserComponent : Component
{
    /// <summary>
    /// Used for events to heretic ritual events to store their results for other methods to use
    /// </summary>
    [DataField, NonSerialized]
    public Dictionary<string, object> Blackboard = new();

    public HereticRitualRaiser Raiser = default!;
}
