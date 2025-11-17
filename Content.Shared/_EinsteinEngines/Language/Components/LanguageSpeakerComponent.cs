// SPDX-FileCopyrightText: 2025 CerberusWolfie <wb.johnb.willis@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._EinsteinEngines.Language.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._EinsteinEngines.Language.Components;

/// <summary>
///     Stores the current state of the languages the entity can speak and understand.
/// </summary>
/// <remarks>
///     All fields of this component are populated during a DetermineEntityLanguagesEvent.
///     They are not to be modified externally.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedLanguageSystem), typeof(SharedTranslatorSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class LanguageSpeakerComponent : Component
{
    public override bool SendOnlyToOwner => true;

    /// <summary>
    ///     The current language the entity uses when speaking.
    ///     Other listeners will hear the entity speak in this language.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype> CurrentLanguage = SharedLanguageSystem.FallbackLanguagePrototype; // The Language system will override it on mapinit

    /// <summary>
    ///     List of languages this entity can speak at the current moment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<LanguagePrototype>> SpokenLanguages = new();

    /// <summary>
    ///     List of languages this entity can understand at the current moment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = new();
}
