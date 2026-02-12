// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Surgery.Conditions;

/// <summary>
/// Checks the target's body against a whitelist and/or blacklist for the surgery to be valid.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBodyComponentConditionComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
