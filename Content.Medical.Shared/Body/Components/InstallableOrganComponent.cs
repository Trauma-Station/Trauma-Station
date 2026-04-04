// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Lets you install an organ by pressing Z, if you don't have one already installed.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(InstallableOrganComponent))]
public sealed partial class InstallableOrganComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4);
}
