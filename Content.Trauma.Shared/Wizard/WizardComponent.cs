// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.Wizard;

[RegisterComponent, NetworkedComponent]
public sealed partial class WizardComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "WizardFaction";
}
