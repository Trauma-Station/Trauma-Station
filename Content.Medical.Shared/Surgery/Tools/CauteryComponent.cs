// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery.Tools;

namespace Content.Medical.Shared.Surgery.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CauteryComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a cautery";
    [DataField]
    public bool? Used { get; set; } = null;
    [DataField, AutoNetworkedField]
    public float Speed { get; set; } = 1f;
}
