// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Traits;
using Content.Server.Radio;
using Content.Trauma.Common.Chat;

namespace Content.Goobstation.Server.Deafness;

public sealed partial class DeafnessSystem : EntitySystem
{
    private EntityQuery<DeafComponent> _deafQuery;

    public override void Initialize()
    {
        base.Initialize();

        _deafQuery = GetEntityQuery<DeafComponent>();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<DeafComponent, ChatMessageOverrideInVoiceRangeEvent>(OnOverrideInVoiceRange);
    }

    private void OnOverrideInVoiceRange(EntityUid uid, DeafComponent comp, ref ChatMessageOverrideInVoiceRangeEvent args)  // blocks normal chat
    {
        args.Cancel();
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args) // blocks radio
    {
        var user = Transform(args.RadioReceiver).ParentUid;

        if (!_deafQuery.HasComp(user))
            return;

        args.Cancelled = true;
    }
}
