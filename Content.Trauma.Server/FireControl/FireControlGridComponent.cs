// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.FireControl;

[RegisterComponent]
public sealed partial class FireControlGridComponent : Component
{
    [ViewVariables]
    public EntityUid? ControllingServer = null;
}
