using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Actions.Components;
using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Rituals;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Robust.Server.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed partial class HereticShapeshiftSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PvsOverrideSystem _pvs = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private PolymorphSystem _poly = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private SharedHereticAbilitySystem _ability = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;

    // We have to delay speech because it doesn't trigger when we do it immediatelly after polymorph
    // Also remove session override that we added to fix ui not closing itself
    private readonly Dictionary<EntityUid, (TimeSpan time, EntityUid oldEnt, string speech)> _delayedShapeshiftEnd = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        foreach (var (newEnt, (time, oldEnt, speech)) in _delayedShapeshiftEnd)
        {
            if (now < time)
                continue;

            var session = CompOrNull<ActorComponent>(newEnt)?.PlayerSession ?? CompOrNull<ActorComponent>(oldEnt)?.PlayerSession;

            if (session is { })
                _pvs.RemoveSessionOverride(oldEnt, session);

            if (!TerminatingOrDeleted(newEnt))
                _chat.TrySendInGameICMessage(newEnt, speech, InGameICChatType.Speak, false);

            _delayedShapeshiftEnd.Remove(newEnt);
        }
    }

    [SubscribeLocalEvent]
    private void OnShapeshiftMessage(Entity<ShapeshiftActionComponent> ent, ref HereticShapeshiftMessage args)
    {
        var key = args.UiKey;
        var user = args.Actor;

        if (!ent.Comp.Polymorphs.Contains(args.ProtoId))
            return;

        if (!CanShapeshift(user))
            return;

        if (!TryComp(user, out ActorComponent? actor))
            return;

        var session = actor.PlayerSession;

        _ui.CloseUi(ent.Owner, key);

        if (!TryComp(ent, out ActionComponent? action) || !_actions.ValidAction((ent, action)))
            return;

        // We have to do this shit because otherwise actor isn't removed from client ui actors list and ui remains
        // open after polymorph
        _pvs.AddSessionOverride(user, session);

        var polymorphed = _poly.PolymorphEntity(user, args.ProtoId);

        _actions.StartUseDelay((ent, action));

        if (polymorphed is not { } uid)
            return;

        _npcFaction.AddFaction(uid, HereticSystem.HereticFactionId);

        if (TryComp(uid, out GhoulComponent? ghoul))
        {
            // Ghoul changes mob threshold and rejuvenates, so we do damage transfer manually after polymorph
            _threshold.TransferDamage(user, uid);
            ghoul.ExamineMessage = null;
            Dirty(uid, ghoul);
        }

        var speech = Loc.GetString(ent.Comp.Speech);
        _delayedShapeshiftEnd[uid] = (_timing.CurTime + TimeSpan.FromMilliseconds(200), user, speech);
    }

    [SubscribeLocalEvent]
    private void OnShapeshift(EventHereticShapeshift args)
    {
        if (!HasComp<ShapeshiftActionComponent>(args.Action))
            return;

        if (!CanShapeshift(args.Performer))
            return;

        if (!_ability.TryUseAbility(args, false))
            return;

        _ui.TryOpenUi(args.Action.Owner, HereticShapeshiftUiKey.Key, args.Performer);
    }

    private bool CanShapeshift(EntityUid user)
    {
        return !TryComp(user, out PolymorphedEntityComponent? polymorphed) || polymorphed.Action == null;
    }
}
