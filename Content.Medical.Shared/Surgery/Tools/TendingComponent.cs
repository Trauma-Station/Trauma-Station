// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery.Tools;

namespace Content.Medical.Shared.Surgery.Tools;

/// <summary>
///     Like Hemostat but lets ghetto tools be used differently for clamping and tending wounds.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TendingComponent : BaseSurgeryToolComponent
{
    public override string ToolName => "a wound tender";
}
