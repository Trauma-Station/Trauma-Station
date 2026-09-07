// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery.Tools;

namespace Content.Medical.Shared.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class DrillComponent : BaseSurgeryToolComponent
{
    public override string ToolName => "a drill";
}
