// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Conversion;
using Content.Goobstation.Shared.Revolutionary;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Dataset;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Revolutionary;

public sealed class RevPropagandaSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    private static readonly ProtoId<LocalizedDatasetPrototype> RevConvertSpeechProto = "RevolutionaryConverterSpeech";
    private static readonly ProtoId<NpcFactionPrototype> Faction = "Revolutionary";
    private static readonly EntProtoId MindRole = "MindRoleRevolutionary";
    private LocalizedDatasetPrototype? _speechLocalization;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevPropagandaComponent, RevPropagandaDoAfterEvent>(OnConvertDoAfter);
        SubscribeLocalEvent<RevPropagandaComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RevPropagandaComponent, AfterInteractEvent>(OnAfterInteract);

        _speechLocalization = _proto.Index<LocalizedDatasetPrototype>(RevConvertSpeechProto);
    }

    private void OnUseInHand(Entity<RevPropagandaComponent> ent, ref UseInHandEvent args)
    {
        if (!SpeakPropaganda(ent, args.User))
            return;

        args.Handled = true;
    }

    private bool SpeakPropaganda(Entity<RevPropagandaComponent> conversionToolEntity, EntityUid user)
    {
        if (_speechLocalization == null
            || _speechLocalization.Values.Count == 0
            || conversionToolEntity.Comp.Silent)
            return false;

        var message = _random.Pick(_speechLocalization);
        _chat.TrySendInGameICMessage(user, Loc.GetString(message), InGameICChatType.Speak, hideChat: false, hideLog: false);
        return true;
    }

    public void OnConvertDoAfter(Entity<RevPropagandaComponent> ent, ref RevPropagandaDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled ||
            args.Target is not { } target ||
            !CanConvert(ent.AsNullable(), user, target) ||
            !_charges.TryUseCharges(ent.Owner, ent.Comp.ConsumesCharges))
            return;

        ConvertTarget(user, target);
    }

    public bool CanConvert(Entity<RevPropagandaComponent?> ent, EntityUid user, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var comp = ent.Comp;

        var ev = new BeforeConversionEvent();
        RaiseLocalEvent(target);
        return !ev.Blocked &&
            TryComp<MindContainerComponent>(target, out var mind) &&
            mind.HasMind &&
            _whitelist.CheckBoth(target, comp.Blacklist, comp.Whitelist) &&
            _whitelist.CheckBoth(user, comp.UserBlacklist, comp.UserWhitelist) &&
            TryComp<HeadRevolutionaryComponent>(user, out var head) &&
            head.ConvertAbilityEnabled &&
            // have to read the propaganda
            _blocker.CanSpeak(user);
    }

    public void ConvertTarget(EntityUid user, EntityUid target)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return;

        _role.MindAddRole(mindId, MindRole);
        // rev shitcode is horrific...
        RemComp<RevolutionEnemyComponent>(target);
        _faction.AddFaction(target, Faction);
        _adminLog.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(user)} converted {ToPrettyString(target)} into a Revolutionary");

        // good boy points
        if (_mind.TryGetMind(user, out var userMindId, out var userMind) &&
            _role.MindHasRole<RevolutionaryRoleComponent>((userMindId, userMind), out var role))
        {
            role.Value.Comp2.ConvertedCount++;
            Dirty(role.Value, role.Value.Comp2);
        }

        // yer a wizard harry
        var comp = EnsureComp<RevolutionaryComponent>(target);

        // let server handle extra stuff
        var ev = new RevConvertedEvent((target, comp), (mindId, mind));
        RaiseLocalEvent(ref ev);
    }

    public void OnAfterInteract(Entity<RevPropagandaComponent> ent, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            args.Target is not { } target ||
            target == user ||
            !args.CanReach ||
            // ignore putting it into a bag or whatever
            !HasComp<MobStateComponent>(target) ||
            !_charges.HasCharges(ent.Owner, ent.Comp.ConsumesCharges))
            return;

        args.Handled = true;
        ConvertDoAfter(ent, target, user);
    }

    private void ConvertDoAfter(Entity<RevPropagandaComponent> converter, EntityUid target, EntityUid user)
    {
        if (user == target)
            return;

        if (!CanConvert(converter.AsNullable(), user, target))
        {
            _popup.PopupClient("You can't convert them!", target, user);
            return;
        }

        SpeakPropaganda(converter, user);

        if (converter.Comp.ConversionDuration == TimeSpan.Zero)
        {
            ConvertTarget(user, target);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            converter.Comp.ConversionDuration,
            new RevPropagandaDoAfterEvent(),
            converter.Owner,
            target: target,
            used: converter.Owner,
            showTo: user)
        {
            Hidden = !converter.Comp.VisibleDoAfter,
            BreakOnMove = false,
            BreakOnWeightlessMove = false,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = false,
        });
    }
}

/// <summary>
/// Event broadcast when a rev gets converted
/// </summary>
[ByRefEvent]
public record struct RevConvertedEvent(Entity<RevolutionaryComponent> Target, Entity<MindComponent> Mind);
