// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Trigger.Components.Conditions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger.Conditions;

/// <summary>
/// Prevents triggering if this entity has no map, or it fails a blacklist/whitelist pair.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MapTriggerConditionSystem))]
[AutoGenerateComponentState]
public sealed partial class MapTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
