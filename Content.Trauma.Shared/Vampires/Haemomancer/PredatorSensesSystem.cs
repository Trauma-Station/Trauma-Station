// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Trauma.Shared.Areas;
using Content.Trauma.Shared.ListEntitySelector;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Vampires.Haemomancer;

public sealed partial class PredatorSensesSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedChatManager _chat = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityQuery<VampireDrainableComponent> _drainableQuery = default!;

    private HashSet<NetEntity> _drainable = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionPredatorSensesComponent, PredatorSensesActionEvent>(OnAction);
        SubscribeLocalEvent<ActionPredatorSensesComponent, ListEntitySelectorMessage>(OnMessage);
    }

    private void OnAction(Entity<ActionPredatorSensesComponent> ent, ref PredatorSensesActionEvent args)
    {
        var performer = args.Performer;

        _drainable.Clear();
        var eqe = EntityQueryEnumerator<ActorComponent, VampireDrainableComponent>();
        while (eqe.MoveNext(out var uid, out _, out _))
        {
            if (uid == performer)
                continue;

            _drainable.Add(GetNetEntity(uid));
        }

        if (_drainable.Count == 0)
        {
            _popup.PopupClient("There is no prey to be hunted here...", performer, PopupType.MediumCaution);
            return;
        }

        _ui.SetUiState(ent.Owner, ListEntitySelectorUiKey.Key, new ListEntitySelectorState(_drainable, "Person to Locate"));
        _ui.TryToggleUi(ent.Owner, ListEntitySelectorUiKey.Key, performer);
    }

    private void OnMessage(Entity<ActionPredatorSensesComponent> ent, ref ListEntitySelectorMessage args)
    {
        // Can't predict for targets outside pvs range
        if (_net.IsClient)
            return;

        var target = GetEntity(args.SelectedEntity);
        if (!_drainableQuery.HasComp(target))
            return;

        if (_action.GetAction(ent.Owner) is not { } action || action.Comp.AttachedEntity is not { } attachedEnt)
            return;

        if (_area.GetArea(target) is not { } area)
        {
            _popup.PopupEntity("They are somewhere away...", attachedEnt, attachedEnt, PopupType.MediumCaution);
            _action.StartUseDelay(action.AsNullable());
            _ui.CloseUi(ent.Owner, ListEntitySelectorUiKey.Key);
            return;
        }

        var msg = $"They are at {Name(area)}.";
        if (_damageable.GetTotalDamage(target) >= ent.Comp.TotalDamage)
        {
            msg += " They are wounded";
        }

        _popup.PopupEntity(msg, attachedEnt, attachedEnt, PopupType.LargeCaution);
        if (TryComp<ActorComponent>(attachedEnt, out var actor))
        {
            var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chat.ChatMessageToOne(ChatChannel.Local, msg,wrappedMsg, EntityUid.Invalid, false, actor.PlayerSession.Channel);
        }

        _action.StartUseDelay(action.AsNullable());
        _ui.CloseUi(ent.Owner,  ListEntitySelectorUiKey.Key);
    }
}

/// <inheritdoc cref="PredatorSensesSystem"/>
public sealed partial class PredatorSensesActionEvent : InstantActionEvent;
