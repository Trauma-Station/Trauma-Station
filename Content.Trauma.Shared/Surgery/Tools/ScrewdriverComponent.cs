// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery.Tools;

namespace Content.Trauma.Shared.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScrewdriverComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "a screwdriver";

    [DataField]
    public bool? Used { get; set; }

    [DataField]
    public float Speed { get; set; } = 1f;
}
