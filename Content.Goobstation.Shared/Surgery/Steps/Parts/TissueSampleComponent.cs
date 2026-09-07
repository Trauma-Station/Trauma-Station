// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery.Tools;

namespace Content.Goobstation.Shared.Surgery.Steps.Parts;

/// <summary>
/// Component for xeno tissue sample, used in the graft issue surgery step.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TissueSampleComponent : BaseSurgeryToolComponent
{
    public override string ToolName => "a xeno tissue sample";
}
