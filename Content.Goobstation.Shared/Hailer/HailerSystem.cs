// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Hailer;

public sealed partial class HailerSystem : EntitySystem
{
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnMapInitEvent(Entity<HailerComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetItemActions(Entity<HailerComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands)
            return;

        args.AddAction(ent.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnHail(Entity<HailerComponent> ent, ref HailerActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.Performer;
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent), GetNetEntity(user));
        var pick = rand.Pick(ent.Comp.Messages);
        _audio.PlayPredicted(pick.Sound, ent, user);
        var name = Identity.Name(user, EntityManager) + " (SecMask)";
        _chat.TrySendInGameICMessage(user, pick.Message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, nameOverride: name, checkRadioPrefix: false);
    }
}
