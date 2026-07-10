// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery.Tools;

namespace Content.Medical.Shared.Surgery.Tools;

/// <summary>
///     It lets you fucking stitch your ass up
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StitchesComponent : BaseSurgeryToolComponent
{
    public override string ToolName => "stitches";
}
