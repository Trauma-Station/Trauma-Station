// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 absurd-shaman <165011607+absurd-shaman@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

// Shitmed Change
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Targeting;

namespace Content.Goobstation.Shared.ReverseBearTrap;

public sealed partial class ReverseBearTrapSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly WoundSystem _wound = default!; // Shitmed Change
    [Dependency] private readonly SharedBodySystem _body = default!; // Shitmed Change

    public static readonly ProtoId<TagPrototype> KeyTag = "ReverseBearTrapKey";
    public static readonly ProtoId<ToolQualityPrototype> Welding = "Welding";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReverseBearTrapComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ReverseBearTrapComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ReverseBearTrapComponent, GetVerbsEvent<Verb>>(OnVerbAdd);

        // DoAfter event handlers
        SubscribeLocalEvent<ReverseBearTrapComponent, BearTrapEscapeDoAfterEvent>(OnBearTrapEscape);
        SubscribeLocalEvent<ReverseBearTrapComponent, BearTrapApplyDoAfterEvent>(OnBearTrapApply);
        SubscribeLocalEvent<ReverseBearTrapComponent, WeldFinishedEvent>(OnWeldFinished);
        SubscribeLocalEvent<ReverseBearTrapComponent, BearTrapUnlockDoAfterEvent>(OnBearTrapUnlock);
    }

    private void OnEquipped(EntityUid uid, ReverseBearTrapComponent trap, GotEquippedEvent args)
    {
        if (args.Slot != "head" || _timing.ApplyingState)
            return;

        ArmTrap(uid, trap, args.Equipee, args.Equipee);
    }

    private void OnMeleeHit(EntityUid uid, ReverseBearTrapComponent trap, MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        // Ensure we're actually hitting a valid target
        if (args.HitEntities.Count == 0 ||
            !HasComp<HumanoidAppearanceComponent>(args.HitEntities.First()) ||
            _inventory.TryGetSlotEntity(args.HitEntities.First(), "head", out _))
            return;

        args.Handled = true;
        var target = args.HitEntities[0];
        var user = args.User;

        _popup.PopupEntity(Loc.GetString("reverse-bear-trap-component-start-cuffing-observer",
                    ("user", Identity.Name(user, EntityManager)), ("target", Identity.Name(target, EntityManager))),
                target, Filter.Pvs(target, entityManager: EntityManager)
                    .RemoveWhere(e => e.AttachedEntity == target || e.AttachedEntity == user), true);

        if (target == user)
        {
            _popup.PopupClient(Loc.GetString("reverse-bear-trap-component-target-self"), user, user);
        }
        else
        {
            _popup.PopupClient(Loc.GetString("reverse-bear-trap-component-start-cuffing-target",
                ("targetName", Identity.Name(target, EntityManager, user))), user, user);
            _popup.PopupEntity(Loc.GetString("reverse-bear-trap-component-start-cuffing-by-other",
                ("otherName", Identity.Name(user, EntityManager, target))), target, target, PopupType.Large);
        }

        _audio.PlayPredicted(trap.StartCuffSound, uid, user);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 3f,
            new BearTrapApplyDoAfterEvent(), uid, target, uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnVerbAdd(EntityUid uid, ReverseBearTrapComponent trap, GetVerbsEvent<Verb> args)
    {
        if (!_actionBlockerSystem.CanComplexInteract(args.User))
            return;

        if (trap.Ticking && trap.Wearer is {} target)
        {
            var activeItem = _handsSystem.GetActiveItem(args.User);
            if (args.User == target)
            {
                args.Verbs.Add(new Verb()
                {
                    Act = () => AttemptEscape(uid, trap, args.User),
                    DoContactInteraction = true,
                    Text = "Attempt escape"
                });
            }
            else
            {
                args.Verbs.Add(new Verb()
                {
                    DoContactInteraction = true,
                    Text = "Remove trap",
                    Disabled = activeItem is not {} tool || !_tool.HasQuality(tool, Welding),
                    Act = () =>
                    {
                        var user = args.User;
                        _popup.PopupEntity(Loc.GetString("reverse-bear-trap-component-start-welding-observer",
                            ("user", Identity.Name(user, EntityManager)), ("target", Identity.Name(target, EntityManager))),
                            target, Filter.Pvs(target, entityManager: EntityManager)
                            .RemoveWhere(e => e.AttachedEntity == target || e.AttachedEntity == user), true);

                        _popup.PopupClient(Loc.GetString("reverse-bear-trap-component-start-welding-target",
                            ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                        _popup.PopupEntity(Loc.GetString("reverse-bear-trap-component-start-welding-by-other",
                            ("otherName", Identity.Name(user, EntityManager, target))), target, target, PopupType.Large);

                        _tool.UseTool(activeItem!.Value, args.User, uid, 5f, Welding, new WeldFinishedEvent(), 3f);
                    }
                });
            }

            if (activeItem is {} key && _tag.HasTag(key, KeyTag))
            {
                args.Verbs.Add(new Verb()
                {
                    DoContactInteraction = true,
                    Text = "Unlock trap",
                    Act = () =>
                    {
                        var user = args.User;
                        _popup.PopupEntity(Loc.GetString("reverse-bear-trap-component-start-unlocking-observer",
                            ("user", Identity.Name(user, EntityManager)), ("target", Identity.Name(target, EntityManager))),
                            target, Filter.Pvs(target, entityManager: EntityManager)
                            .RemoveWhere(e => e.AttachedEntity == target || e.AttachedEntity == user), true);

                        if (target == user)
                        {
                            _popup.PopupClient(Loc.GetString("reverse-bear-trap-component-start-unlocking-target-self"), user, user);
                        }
                        else
                        {
                            _popup.PopupClient(Loc.GetString("reverse-bear-trap-component-start-unlocking-target",
                                ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                            _popup.PopupEntity(Loc.GetString("reverse-bear-trap-component-start-unlocking-by-other",
                                ("otherName", Identity.Name(user, EntityManager, target))), target, target, PopupType.Large);
                        }

                        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 1.5f,
                            new BearTrapUnlockDoAfterEvent(), uid, uid)
                        {
                            BreakOnDamage = true,
                            BreakOnMove = true,
                            AttemptFrequency = AttemptFrequency.EveryTick
                        };

                        _doAfter.TryStartDoAfter(doAfterArgs);
                    }
                });
            }
        }
        else
        {
            if (trap.DelayOptions == null || trap.DelayOptions.Count == 1)
                return;

            foreach (var option in trap.DelayOptions)
            {
                var secs = option.TotalSeconds;
                if (MathHelper.CloseTo(secs, trap.CountdownDuration.TotalSeconds))
                {
                    args.Verbs.Add(new Verb()
                    {
                        Category = TimerOptions,
                        Text = Loc.GetString("verb-trigger-timer-set-current", ("time", option)),
                        Disabled = true,
                        Priority = (int) (-100 * secs)
                    });
                    continue;
                }

                args.Verbs.Add(new Verb()
                {
                    Category = TimerOptions,
                    Text = Loc.GetString("verb-trigger-timer-set", ("time", option)),
                    Priority = (int) (-100 * secs),

                    Act = () =>
                    {
                        trap.CountdownDuration = option;
                        Dirty(uid, trap);
                        _popup.PopupClient(Loc.GetString("popup-trigger-timer-set", ("time", option)), args.User, args.User);
                    },
                });
            }
        }
    }

    private void OnBearTrapEscape(EntityUid uid, ReverseBearTrapComponent trap, BearTrapEscapeDoAfterEvent args)
    {
        trap.Struggling = false;

        // TODO: predict this shit lmao
        if (args.Cancelled || trap.Wearer is not {} target)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(uid).Id);
        var rand = new System.Random(seed);

        var prefix = "";
        if (rand.Prob(trap.CurrentEscapeChance))
        {
            ResetTrap(uid, trap);
        }
        else
        {
            prefix = "failed-";
            trap.CurrentEscapeChance += 0.25f;
        }

        var identity = Identity.Name(target, EntityManager);
        var you = Loc.GetString($"reverse-bear-trap-component-{prefix}unlocked-trap-self");
        var others = Loc.GetString($"reverse-bear-trap-component-{prefix}unlocked-trap-observer", ("user", identity));
        _popup.PopupPredicted(you, others, target, args.User);
    }

    private void OnBearTrapApply(EntityUid uid, ReverseBearTrapComponent trap, BearTrapApplyDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target || args.Used is not { } used)
            return;

        if (!_inventory.TryGetSlotEntity(target, "head", out var _)
            && _inventory.TryEquip(target, used, "head", true, true))
            ArmTrap(used, trap, target, args.User);
    }

    private void OnWeldFinished(EntityUid uid, ReverseBearTrapComponent trap, WeldFinishedEvent args)
    {
        if (args.Cancelled || trap.Wearer is not {} target)
            return;

        var user = args.User;
        _damageable.ChangeDamage(target, trap.WeldDamage, true, origin: user, targetPart: TargetBodyPart.Head);

        var identity = Identity.Name(target, EntityManager);
        var you = Loc.GetString("reverse-bear-trap-component-trap-fall-self");
        var others = Loc.GetString("reverse-bear-trap-component-trap-fall-observer", ("user", identity));
        _popup.PopupPredicted(you, others, uid, user);

        ResetTrap(uid, trap);
    }

    private void OnBearTrapUnlock(EntityUid uid, ReverseBearTrapComponent trap, BearTrapUnlockDoAfterEvent args)
    {
        if (args.Cancelled || trap.Wearer is not {} wearer || !_timing.IsFirstTimePredicted)
            return;

        _audio.PlayPredicted(trap.StartCuffSound, wearer, args.User);

        var identity = Identity.Name(wearer, EntityManager);
        var you = Loc.GetString("reverse-bear-trap-component-trap-fall-self");
        var others = Loc.GetString("reverse-bear-trap-component-trap-fall-observer", ("user", identity));
        _popup.PopupPredicted(you, others, wearer, args.User);

        ResetTrap(uid, trap);
    }

    private void ArmTrap(EntityUid uid, ReverseBearTrapComponent trap, EntityUid wearer, EntityUid user)
    {
        if (trap.Ticking || !Exists(wearer) || !_interaction.InRangeUnobstructed(uid, wearer))
            return;

        trap.NextTrigger = _timing.CurTime + trap.CountdownDuration;
        trap.Wearer = wearer;
        trap.CurrentEscapeChance = trap.BaseEscapeChance;
        EnsureComp<UnremoveableComponent>(uid);

        Dirty(uid, trap);

        _audio.PlayPredicted(trap.BeepSound, uid, user,
            AudioParams.Default.WithVolume(-5f));

        if (_net.IsServer)
        {
            trap.LoopSoundStream = _audio.PlayPvs(trap.LoopSound, uid,
                AudioParams.Default.WithLoop(true))?.Entity;
        }

        var identity = Identity.Entity(wearer, EntityManager);
        var you = Loc.GetString("reverse-bear-trap-component-trap-click-self");
        var others = Loc.GetString("reverse-bear-trap-component-trap-click-observer", ("user", identity));
        _popup.PopupPredicted(you, others, wearer, wearer);
    }

    private void ResetTrap(EntityUid uid, ReverseBearTrapComponent trap)
    {
        if (!trap.Ticking)
            return;

        var oldWearer = trap.Wearer;

        trap.LoopSoundStream = _audio.Stop(trap.LoopSoundStream);
        trap.NextTrigger = null;
        trap.Wearer = null;
        trap.Struggling = false;
        trap.CurrentEscapeChance = trap.BaseEscapeChance;
        RemComp<UnremoveableComponent>(uid);

        Dirty(uid, trap);

        if (oldWearer != null)
            _inventory.TryUnequip(oldWearer.Value, "head", true, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ReverseBearTrapComponent>();
        while (query.MoveNext(out var uid, out var trap))
        {
            if (!trap.Ticking || trap.Wearer == null)
                continue;

            if (now >= trap.NextTrigger)
                SnapTrap(uid, trap);
        }
    }

    private void SnapTrap(EntityUid uid, ReverseBearTrapComponent? trap)
    {
        if (!Resolve(uid, ref trap) || trap.Wearer is not {} wearer)
            return;

        _audio.PlayPredicted(trap.SnapSound, wearer, wearer);

        var you = Loc.GetString("reverse-bear-trap-component-trap-snap-self");
        var others = Loc.GetString("reverse-bear-trap-component-trap-snap-observer", ("user", Identity.Name(wearer, EntityManager)));
        _popup.PopupPredicted(you, others, wearer, wearer, PopupType.LargeCaution);

        // damage destroys trap
        ResetTrap(uid, trap);

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", 300);
        _damageable.TryChangeDamage(wearer, damage, true, origin: uid, targetPart: TargetBodyPart.Head);
        var head = _body.GetBodyChildrenOfType(wearer, BodyPartType.Head).FirstOrDefault();
        if (head != default
            && TryComp<WoundableComponent>(head.Id, out var woundable)
            && woundable.ParentWoundable.HasValue)
            _wound.AmputateWoundable(woundable.ParentWoundable.Value, head.Id, woundable);
    }

    private void AttemptEscape(EntityUid uid, ReverseBearTrapComponent trap, EntityUid user)
    {
        if (trap.Struggling)
            return;

        trap.Struggling = true;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, 6f,
            new BearTrapEscapeDoAfterEvent(), uid, user)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [Serializable, NetSerializable]
    private sealed partial class BearTrapEscapeDoAfterEvent : SimpleDoAfterEvent;
    [Serializable, NetSerializable]
    private sealed partial class BearTrapApplyDoAfterEvent : SimpleDoAfterEvent;
    [Serializable, NetSerializable]
    private sealed partial class BearTrapUnlockDoAfterEvent : SimpleDoAfterEvent;

    private static readonly VerbCategory TimerOptions = new("verb-categories-timer", "/Textures/Interface/VerbIcons/clock.svg.192dpi.png");
}
