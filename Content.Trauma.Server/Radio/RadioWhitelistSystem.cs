// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Whitelist;

namespace Content.Trauma.Server.Radio;

public sealed partial class RadioWhitelistSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void OnIntrinsicReceiveAttempt(Entity<IntrinsicRadioReceiverComponent> ent, ref RadioReceiveAttemptEvent args)
    {
        args.Cancelled = _whitelist.IsWhitelistFail(args.Channel.ReceiveWhitelist, ent.Owner);
    }
}
