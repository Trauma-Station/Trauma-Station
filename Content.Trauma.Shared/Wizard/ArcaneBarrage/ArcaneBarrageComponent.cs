// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Wizard.ArcaneBarrage;

[RegisterComponent, NetworkedComponent]
public sealed partial class ArcaneBarrageComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Unremoveable = true;
}
