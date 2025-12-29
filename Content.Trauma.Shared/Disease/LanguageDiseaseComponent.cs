// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._EinsteinEngines.Language;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Disease;

/// <summary>
/// Component for disease entities that will make them trigger an infection once the host speaks a language.
/// aka MGSV Vocal Cord Parasites.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LanguageDiseaseSystem))]
[AutoGenerateComponentState]
public sealed partial class LanguageDiseaseComponent : Component
{
    /// <summary>
    /// Languages the host has to speak to trigger, or not to speak when inverted.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public HashSet<ProtoId<LanguagePrototype>> Languages = new();

    /// <summary>
    /// What to set infection rate to when triggered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TriggerInfectionRate = 0.02f;

    /// <summary>
    /// Invert logic of triggering, it will trigger if you speak any language except the ones in <see cref="Languages"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Inverted;
}
