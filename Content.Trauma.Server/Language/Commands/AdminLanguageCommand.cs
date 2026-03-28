// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Shared.Language.Components;
using Content.Trauma.Shared.Language.Systems;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Trauma.Server.Language.Commands;

[ToolshedCommand(Name = "language"), AdminCommand(AdminFlags.Admin)]
public sealed class AdminLanguageCommand : ToolshedCommand
{
    private LanguageSystem? _languagesField;
    private LanguageSystem Languages => _languagesField ??= GetSys<LanguageSystem>();

    [CommandImplementation("add")]
    public EntityUid AddLanguage(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool canSpeak = true,
        [CommandArgument] bool canUnderstand = true
    )
    {
        if (language == SharedLanguageSystem.UniversalPrototype)
        {
            EnsureComp<UniversalLanguageSpeakerComponent>(input);
            Languages.UpdateEntityLanguages(input);
        }
        else
        {
            EnsureComp<LanguageSpeakerComponent>(input);
            Languages.AddLanguage(input, language, canSpeak, canUnderstand);
        }

        return input;
    }

    [CommandImplementation("rm")]
    public EntityUid RemoveLanguage(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<LanguagePrototype> language,
        [CommandArgument] bool removeSpeak = true,
        [CommandArgument] bool removeUnderstand = true
    )
    {
        if (language == SharedLanguageSystem.UniversalPrototype && HasComp<UniversalLanguageSpeakerComponent>(input))
        {
            RemComp<UniversalLanguageSpeakerComponent>(input);
            EnsureComp<LanguageSpeakerComponent>(input);
        }
        // We execute this branch even in case of universal so that it gets removed if it was added manually to the LanguageSystem.
        Languages.RemoveLanguage(input, language, removeSpeak, removeUnderstand);

        return input;
    }

    [CommandImplementation("lsspoken")]
    public IEnumerable<ProtoId<LanguagePrototype>> ListSpoken([PipedArgument] EntityUid input)
    {
        return Languages.GetSpokenLanguages(input);
    }

    [CommandImplementation("lsunderstood")]
    public IEnumerable<ProtoId<LanguagePrototype>> ListUnderstood([PipedArgument] EntityUid input)
    {
        return Languages.GetUnderstoodLanguages(input);
    }
}
