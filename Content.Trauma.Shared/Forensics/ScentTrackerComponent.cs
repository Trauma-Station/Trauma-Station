// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Forensics;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScentTrackerComponent : Component
{
    /// <summary>
    /// The currently tracked scent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Scent = string.Empty;

    /// <summary>
    /// The time that it takes to sniff an entity.
    /// </summary>
    [DataField]
    public TimeSpan SniffDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When the client will next spawn scent effects.
    /// </summary>
    [ViewVariables]
    public TimeSpan NextEffects;
}
