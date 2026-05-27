// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Trauma.Server.GameTicking.Rules.Components;
using Content.Trauma.Shared.Morph;

namespace Content.Trauma.Server.GameTicking.Rules;

/// <summary>
/// Adds morph to the round end summary.
/// </summary>
public sealed partial class MorphRuleSystem : GameRuleSystem<MorphRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorphRuleComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    /// <summary>
    /// Checks for "prime morphs" and looks how many times they replicated, would do all morphs but that would clog the end of round brief a bit.
    /// </summary>
    protected override void AppendRoundEndText(EntityUid uid, MorphRuleComponent comp, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, comp, gameRule, ref args);

        var sessionData = _antag.GetAntagIdentifiers(uid);
        var lone = sessionData.Count == 1;
        foreach (var (mindId, data, name) in sessionData)
        {
            var count = 0;
            // TODO: store it on the mind role instead
            if (TryComp<MindComponent>(mindId, out var mind) &&
                GetEntity(mind.OriginalOwnedEntity) is {} mob &&
                TryComp<MorphComponent>(mob, out var morph))
                count = morph.Children;

            var key = lone ? "morph-name-user-lone" : "morph-name-user";
            args.AddLine(Loc.GetString(key, ("name", name), ("username", data.UserName), ("count", count)));
        }
    }

    private void OnAntagSelected(Entity<MorphRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        Comp<MorphComponent>(args.EntityUid).Rule = ent;
    }
}
