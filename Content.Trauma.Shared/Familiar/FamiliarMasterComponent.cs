// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Familiar;

/// <summary>
/// Given to a mind, mob or ghost role that serves a certain master.
/// Tells them who their master is in the character menu.
/// Automatically gets transferred to the mob when taking over a ghost role with this component.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(FamiliarSystem))]
[AutoGenerateComponentState]
public sealed partial class FamiliarMasterComponent : Component
{
    /// <summary>
    /// The master that summoned this familiar.
    /// Can be either a mind or the mob itself if it has none.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Master;

    /// <summary>
    /// The name of <see cref="Master"/> at the time of summoning.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string MasterName = string.Empty;
}
