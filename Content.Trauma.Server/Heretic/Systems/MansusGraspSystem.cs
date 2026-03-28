// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Tag;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Heretic.Rituals;
using Content.Trauma.Shared.Heretic.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class MansusGraspSystem : SharedMansusGraspSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly EntProtoId RitualRune = "HereticRuneRitual";
    private static readonly EntProtoId RitualAnimation = "HereticRuneRitualDrawAnimation";

    private static readonly List<ProtoId<TagPrototype>> PenTags = ["Pen", "Write"];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TagComponent, AfterInteractEvent>(OnAfterInteract, before: [typeof(TouchSpellSystem)]);
        SubscribeLocalEvent<DrawRitualRuneDoAfterEvent>(OnRitualRuneDoAfter);
        SubscribeLocalEvent<StatusEffectContainerComponent, ParentPacketReceiveAttemptEvent>(OnPacket);
        SubscribeLocalEvent<MansusGraspUpgradeComponent, AfterTouchSpellAbilityUsedEvent>(OnAfterTouchSpell);
    }

    private void OnAfterTouchSpell(Entity<MansusGraspUpgradeComponent> ent, ref AfterTouchSpellAbilityUsedEvent args)
    {
        EntityManager.AddComponents(args.TouchSpell, ent.Comp.AddedComponents);
    }

    private void OnPacket(Entity<StatusEffectContainerComponent> ent, ref ParentPacketReceiveAttemptEvent args)
    {
        if (Status.HasStatusEffect(ent, GraspAffectedStatus))
            args.Cancelled = true;
    }

    private void OnAfterInteract(Entity<TagComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach
            || !args.ClickLocation.IsValid(EntityManager)
            || !_heretic.TryGetHereticComponent(args.User, out _, out _) // not a heretic - how???
            || HasComp<ActiveDoAfterComponent>(args.User)) // prevent rune shittery
            return;

        var runeProto = RitualAnimation;
        float time = 14;

        var canScribe = true;
        if (TryComp(ent, out TransmutationRuneScriberComponent? scriber)) // if it is special rune scriber
        {
            canScribe = _toggle.IsActivated(ent.Owner);
            runeProto = scriber.RuneDrawingEntity ?? runeProto;
            time = scriber.Time ?? time;
        }
        else if (TouchSpell.FindTouchSpell(args.User, GraspWhitelist) == null || // No grasp
                 !_tag.HasAnyTag(ent.Comp, PenTags)) // not a pen
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
            new DrawRitualRuneDoAfterEvent(GetNetEntity(rune), GetNetCoordinates(args.ClickLocation)),
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
        QueueDel(GetEntity(ev.RitualRune));

        if (!ev.Cancelled)
            _transform.AttachToGridOrMap(Spawn(RitualRune, GetCoordinates(ev.Coords)));
    }
}
