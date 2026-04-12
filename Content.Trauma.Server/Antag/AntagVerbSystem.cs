// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Systems;
using Content.Server.Antag;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Antag;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Trauma.Server.Antag;

public sealed class AntagVerbSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetAntagVerbsEvent>(OnGetAntagVerbs);
    }

    private void OnGetAntagVerbs(ref GetAntagVerbsEvent args)
    {
        var session = args.Session;
        var verbs = args.Verbs.Verbs;

        foreach (var smite in _proto.EnumeratePrototypes<AntagSmitePrototype>().OrderBy(p => p.ID))
        {
            if (!_whitelist.CheckBoth(args.Target, blacklist: smite.Blacklist, whitelist: smite.Whitelist))
                continue;

            var antag = _proto.Index(smite.Antag);
            var name = Loc.GetString(antag.Name);
            verbs.Add(new Verb()
            {
                Text = Loc.GetString("admin-verb-text-make-antag", ("antag", name)),
                Category = VerbCategory.Antag,
                Icon = smite.Icon,
                Act = () =>
                {
                    _antag.ForceMakeAntag(session, smite.Rule, smite.RuleComp);
                },
                Impact = LogImpact.High,
                Message = Loc.GetString("admin-verb-make-antag", ("antag", name))
            });
        }
    }
}
