// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery.Tools;
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class DrillComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a drill";
    [DataField]
    public bool? Used { get; set; } = null;
    [DataField]
    public float Speed { get; set; } = 1f;
}
