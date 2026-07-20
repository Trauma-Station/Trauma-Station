// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Common.StationEvents;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.Language.Systems;

namespace Content.Trauma.Server.StationEvents;

/// <summary>
/// Makes sure random sentience targets can speak/understand Tau Ceti Basic.
/// </summary>
public sealed partial class RandomSentienceLanguageSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private SharedLanguageSystem _language = default!;

    public static readonly ProtoId<LanguagePrototype> TauCetiBasic = "TauCetiBasic";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomSentienceEvent>(OnRandomSentience);
    }

    private void OnRandomSentience(ref RandomSentienceEvent args)
    {
        var target = args.Target;
        _knowledge.EnsureKnowledgeContainer(target);
        var comp = EnsureComp<LanguageSpeakerComponent>(target);
        _language.AddLanguage(target, TauCetiBasic);
    }
}
