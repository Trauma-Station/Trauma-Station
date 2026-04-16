// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent]
public sealed partial class EnvyKnifeComponent : Component
{
    [DataField]
    public EntProtoId RevertAction = "DisguiseRevertAction";
}
