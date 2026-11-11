// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.Samurai;

/// <summary>
/// Component for Samurai armor abilities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SamuraiArmorComponent : Component
{
    /// <summary>
    /// The action id for recalling a bound energy polearm
    /// </summary>
    [DataField]
    public EntProtoId RecallEnergyPolearmAction = "ActionRecallEnergyPolearm";

    [DataField, AutoNetworkedField]
    public EntityUid? RecallEnergyPolearmActionEntity;
}

public sealed partial class RecallEnergyPolearmEvent : InstantActionEvent;
