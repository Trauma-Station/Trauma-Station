// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Blob;
using Content.Server.Administration.Systems;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.Administration.Systems;

public sealed partial class GoobAntagSmiteSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGetAntagVerbs(ref GetAntagVerbsEvent args)
    {
        var target = args.Target;
        args.Verbs.Verbs.Add(new()
        {
            Text = Loc.GetString("admin-verb-text-make-blob"),
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("_Goobstation/Blob/Actions/blob.rsi"), "blobFactory"),
            Act = () =>
            {
                EnsureComp<BlobCarrierComponent>(target).HasMind = HasComp<ActorComponent>(target);
            },
            Impact = LogImpact.High,
            Message = Loc.GetString("admin-verb-text-make-blob"),
        });
    }
}
