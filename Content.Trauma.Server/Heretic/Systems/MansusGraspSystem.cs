// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Tag;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Rituals;
using Content.Trauma.Shared.Heretic.Systems;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class MansusGraspSystem : SharedMansusGraspSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public static readonly LocId DefaultInvocation = "heretic-speech-mansusgrasp";

    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TagComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DrawRitualRuneDoAfterEvent>(OnRitualRuneDoAfter);
        SubscribeLocalEvent<StatusEffectContainerComponent, ParentPacketReceiveAttemptEvent>(OnPacket);
    }

    private void OnPacket(Entity<StatusEffectContainerComponent> ent, ref ParentPacketReceiveAttemptEvent args)
    {
        if (Status.HasStatusEffect(ent, GraspAffectedStatus))
            args.Cancelled = true;
    }

    public override void InvokeGrasp(EntityUid user, Entity<MansusGraspComponent>? ent)
    {
        base.InvokeGrasp(user, ent);

        var invocation = ent == null ? DefaultInvocation : ent.Value.Comp.Invocation;

        if (invocation == null)
            return;

        _chat.TrySendInGameICMessage(user, Loc.GetString(invocation), InGameICChatType.Speak, false);
    }

    private void OnAfterInteract(Entity<TagComponent> ent, ref AfterInteractEvent args)
    {
        var tags = ent.Comp.Tags;

        if (!args.CanReach
            || !args.ClickLocation.IsValid(EntityManager)
            || !_heretic.TryGetHereticComponent(args.User, out var heretic, out _) // not a heretic - how???
            || HasComp<ActiveDoAfterComponent>(args.User)) // prevent rune shittery
            return;

        var runeProto = "HereticRuneRitualDrawAnimation";
        float time = 14;

        var canScribe = true;
        if (TryComp(ent, out TransmutationRuneScriberComponent? scriber)) // if it is special rune scriber
        {
            canScribe = _toggle.IsActivated(ent.Owner);
            runeProto = scriber.RuneDrawingEntity ?? runeProto;
            time = scriber.Time ?? time;
        }
        else if (heretic.MansusGraspAction == EntityUid.Invalid // no grasp - not special
                 || !tags.Contains("Write") || !tags.Contains("Pen")) // not a pen
            return;

        // remove our rune if clicked
        if (args.Target != null && HasComp<HereticRitualRuneComponent>(args.Target))
        {
            args.Handled = true;
            // todo: add more fluff
            QueueDel(args.Target);
            return;
        }

        if (!canScribe)
            return;

        args.Handled = true;

        // spawn our rune
        var rune = Spawn(runeProto, args.ClickLocation);
        _transform.AttachToGridOrMap(rune);
        var dargs = new DoAfterArgs(EntityManager,
            args.User,
            time,
            new DrawRitualRuneDoAfterEvent(rune, args.ClickLocation),
            args.User)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            CancelDuplicate = false,
            MultiplyDelay = false,
            Broadcast = true,
        };
        _doAfter.TryStartDoAfter(dargs);
    }

    private void OnRitualRuneDoAfter(DrawRitualRuneDoAfterEvent ev)
    {
        // delete the animation rune regardless
        QueueDel(ev.RitualRune);

        if (!ev.Cancelled)
            _transform.AttachToGridOrMap(Spawn("HereticRuneRitual", ev.Coords));
    }
}
