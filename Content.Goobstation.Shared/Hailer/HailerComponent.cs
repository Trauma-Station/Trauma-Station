// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Hailer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HailerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId HailerAction = "ActionHailer";

    [DataField, AutoNetworkedField]
    public EntityUid? HailActionEntity;
}
