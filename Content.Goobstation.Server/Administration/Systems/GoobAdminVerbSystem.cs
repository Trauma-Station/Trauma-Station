// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Systems;
using Content.Shared.Verbs;

namespace Content.Goobstation.Server.Administration.Systems;

public sealed partial class GoobAdminVerbSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetAntagVerbsEvent>(OnGetAntagVerbs);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddSmiteVerbs);
    }
}
