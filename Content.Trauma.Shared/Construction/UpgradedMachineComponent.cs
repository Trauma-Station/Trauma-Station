// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Construction;

/// <summary>
/// Component added to machines to prevent stacking upgrades and show what upgrade they have.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(UpgradedMachineSystem))]
[AutoGenerateComponentState]
public sealed partial class UpgradedMachineComponent : Component
{
    /// <summary>
    /// The string to show when examined.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId Upgrade;
}
