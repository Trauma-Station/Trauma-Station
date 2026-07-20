// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Language.Systems;
using Content.Trauma.Common.Knowledge.Systems;

namespace Content.Trauma.Common.Language.Components;

/// <summary>
///     Stores the current state of the languages the entity can speak and understand.
/// </summary>
/// <remarks>
///     All fields of this component are populated during a DetermineEntityLanguagesEvent.
///     They are not to be modified externally.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(CommonLanguageSystem), typeof(CommonTranslatorSystem), typeof(CommonKnowledgeSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class LanguageSpeakerComponent : Component
{
    public override bool SendOnlyToOwner => true;

    /// <summary>
    ///     The current language the entity uses when speaking.
    ///     Other listeners will hear the entity speak in this language.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype> CurrentLanguage = CommonLanguageSystem.FallbackLanguagePrototype; // The Language system will override it on mapinit

    /// <summary>
    ///     List of languages this entity can speak at the current moment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<LanguagePrototype>> Speaks = new();

    /// <summary>
    ///     List of languages this entity can understand at the current moment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<LanguagePrototype>> Understands = new();
}
