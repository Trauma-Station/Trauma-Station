// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Wizard.Components;

[RegisterComponent]
public sealed partial class WizardRuleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? TargetStation;
}
