// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Medical.Shared.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryStepCavityEffectComponent : Component
{
    [DataField]
    public string Action = "Insert";
}
