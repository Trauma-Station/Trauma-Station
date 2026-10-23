// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Components;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Heretic.Components.Side;

/// <summary>
/// Adds "recall item" action to user when they pick up item with this component
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecallableItemComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<ActionComponent> ActionId;

    [DataField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? User;

    [DataField]
    public EntityWhitelist? UserWhitelist;

    [DataField]
    public EntityWhitelist? UserBlacklist;

    [DataField]
    public bool WhitelistCheckMind;
}
