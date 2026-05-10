// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.Wizard.Systems;

namespace Content.Trauma.Server.Wizard.Components;

[RegisterComponent, Access(typeof(WizardRuleSystem))]
public sealed partial class RuleOnWizardDeathRuleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Rule = default!;
}
