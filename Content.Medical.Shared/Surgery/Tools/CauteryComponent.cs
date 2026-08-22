// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery.Tools;

namespace Content.Medical.Shared.Surgery.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CauteryComponent : BaseSurgeryToolComponent
{
    public override string ToolName => "a cautery";
    [AutoNetworkedField]
    public new float Speed
    {
        get
        {
            return base.Speed;
        }
        set
        {
            base.Speed = value;
        }
    }
}
