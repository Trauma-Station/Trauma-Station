// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Popups;
using Content.Shared.Speech;

namespace Content.Medical.Shared.Body;

public sealed class NeedsTongueSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<EnabledOrganComponent> _enabledQuery;

    public override void Initialize()
    {
        base.Initialize();

        _enabledQuery = GetEntityQuery<EnabledOrganComponent>();

        SubscribeLocalEvent<NeedsTongueComponent, SpeakAttemptEvent>(OnSpeakAttempt);
    }

    private void OnSpeakAttempt(Entity<NeedsTongueComponent> ent, ref SpeakAttemptEvent args)
    {
        if (args.Cancelled || _body.GetOrgan(ent.Owner, ent.Comp.Category) is {} tongue && _enabledQuery.HasComp(tongue))
            return;

        // TODO: change to PopupClient if chat gets predicted
        _popup.PopupEntity(Loc.GetString("speech-muted"), ent, ent);
        args.Cancel();
    }
}
