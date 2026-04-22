// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Wizard.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CurseOfByondComponent : Component
{
    [DataField]
    public string CurseOfByondAlertKey = "CurseOfByond";
}
