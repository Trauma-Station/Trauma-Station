// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Prototypes;

namespace Content.Trauma.Shared.Wizard.MagicMirror;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WizardMirrorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField(required: true)]
    public HashSet<ProtoId<SpeciesPrototype>> AllowedSpecies = new();
}
