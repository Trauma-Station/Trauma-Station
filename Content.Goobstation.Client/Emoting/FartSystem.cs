// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Emoting;
using Content.Shared.Chat.Prototypes;

namespace Content.Goobstation.Client.Emoting;

public sealed partial class FartSystem : SharedFartSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FartComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, FartComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not FartComponentState state ||
            !ProtoMan.Resolve(state.Emote, out var emote))
            return;

        if (emote.Event != null)
            RaiseLocalEvent(uid, emote.Event);
    }
}
