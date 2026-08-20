// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Traits;
using Content.Server.Radio;
using Content.Trauma.Common.Chat;

namespace Content.Goobstation.Server.Deafness;

public sealed partial class DeafnessSystem : EntitySystem
{
    [Dependency] private EntityQuery<DeafComponent> _query = default!;

    [SubscribeLocalEvent]
    private void OnOverrideInVoiceRange(EntityUid uid, DeafComponent comp, ref ChatMessageOverrideInVoiceRangeEvent args)
    {
        // blocks normal chat
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        var user = Transform(args.RadioReceiver).ParentUid;
        if (!_query.HasComp(user))
            return;

        // blocks radio
        args.Cancelled = true;
    }
}
