// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._White.BackStab;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Gibbing;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Systems;
using Content.Trauma.Shared.Heretic.Systems.Abilities;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Cosmos;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Heretic.Rituals;

public abstract partial class SharedHereticRitualSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly HereticRitualEffectSystem _effects = default!;
    [Dependency] private readonly BackStabSystem _backStab = default!;
    [Dependency] private readonly SharedStarMarkSystem _starMark = default!;
    [Dependency] private readonly SharedMansusGraspSystem _grasp = default!;
    [Dependency] private readonly SharedHereticAbilitySystem _ability = default!;

    public SoundSpecifier RitualSuccessSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/castsummon.ogg");

    private EntityQuery<GhoulComponent> _ghoulQuery;
    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<TagComponent> _tagQuery;

    public const string Performer = "Performer";
    public const string Mind = "Mind";
    public const string Platform = "Platform";
    public const string CancelString = "CancelString";
    public const string SuccessOverride = "SuccessOverride";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualRuneComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticRitualRuneComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<HereticRitualRuneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HereticRitualRuneComponent, HereticRitualMessage>(OnRitualChosenMessage);

        _ghoulQuery = GetEntityQuery<GhoulComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();
        _tagQuery = GetEntityQuery<TagComponent>();

        SubscribeConditions();
        SubscribeEffects();
    }

    #region Helpers

    protected virtual (bool isCommand, bool isSec) IsCommandOrSec(EntityUid uid)
    {
        return (false, false);
    }

    private bool IsSacrificeTarget(Entity<Components.HereticComponent> heretic, EntityUid target)
    {
        return heretic.Comp.SacrificeTargets.Any(x => x.Entity == GetNetEntity(target));
    }

    private void CancelCondition<T>(Entity<HereticRitualRaiserComponent> ent,
        ref HereticRitualConditionEvent<T> ev,
        string? cancelString = null)
        where T : BaseRitualCondition<T>
    {
        ev.Result = false;

        if (cancelString != null)
            ent.Comp.Blackboard[CancelString] = cancelString;
    }

    public bool TryGetValue<T>(Entity<HereticRitualRaiserComponent> ent, string key, [NotNullWhen(true)] out T? value)
    {
        if (ent.Comp.Blackboard.TryGetValue(key, out var val))
        {
            value = (T) val;
            return true;
        }

        value = default;
        return false;
    }

    private bool TryDoRitual(Entity<HereticRitualRaiserComponent, HereticRitualComponent> ent, EntityUid user)
    {
        bool result;
        if (ent.Comp2.Limit > 0)
        {
            ent.Comp2.LimitedOutput = ent.Comp2.LimitedOutput.Where(Exists).ToList();
            if (ent.Comp2.LimitedOutput.Count >= ent.Comp2.Limit)
            {
                if (ent.Comp2.LimitReachedEffects is { } limitReachedEffects)
                    result = _effects.TryEffects(ent, limitReachedEffects, ent, user);
                else
                {
                    ent.Comp1.Blackboard[CancelString] = Loc.GetString("heretic-ritual-fail-limit");
                    return false;
                }
            }
            else
                result = _effects.TryEffects(ent, ent.Comp2.Effects, ent, user);
        }
        else
            result = _effects.TryEffects(ent, ent.Comp2.Effects, ent, user);

        if (TryGetValue(ent, SuccessOverride, out bool overrideSuccess))
            result = overrideSuccess;
        return result;
    }

    private void SetupBlackboard(Entity<HereticRitualRaiserComponent, HereticRitualComponent> ent,
        EntityUid performer,
        EntityUid mind,
        EntityUid platform)
    {
        ent.Comp1.Blackboard.Clear();
        ent.Comp1.Blackboard[Performer] = performer;
        ent.Comp1.Blackboard[Mind] = mind;
        ent.Comp1.Blackboard[Platform] = platform;
        if (ent.Comp2.CancelLoc is { } loc)
            ent.Comp1.Blackboard[CancelString] = Loc.GetString(loc);
    }

    #endregion

    #region RitualRuneEvents

    private void OnInteract(Entity<HereticRitualRuneComponent> ent, ref InteractHandEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        if (heretic.RitualContainer.Count == 0)
        {
            _popup.PopupClient(Loc.GetString("heretic-ritual-norituals"), args.User, args.User);
            return;
        }

        _uiSystem.OpenUi(ent.Owner, HereticRitualRuneUiKey.Key, args.User);
    }

    private void OnRitualChosenMessage(Entity<HereticRitualRuneComponent> ent, ref HereticRitualMessage args)
    {
        var user = args.Actor;

        if (!_heretic.TryGetHereticComponent(user, out var heretic, out var mind))
            return;

        var ritual = GetEntity(args.Ritual);

        if (!heretic.RitualContainer.Contains(ritual))
            return;

        heretic.ChosenRitual = ritual;
        Dirty(mind, heretic);

        var ritualName = Name(ritual);
        _popup.PopupClient(Loc.GetString("heretic-ritual-switch", ("name", ritualName)), user, user);
    }

    private void OnInteractUsing(Entity<HereticRitualRuneComponent> ent, ref InteractUsingEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out var mind))
            return;

        if (!HasComp<Components.MansusGraspComponent>(args.Used))
            return;

        if (!TryComp(heretic.ChosenRitual, out HereticRitualComponent? ritual))
        {
            _popup.PopupClient(Loc.GetString("heretic-ritual-noritual"), args.User, args.User);
            return;
        }

        var raiser = EnsureComp<HereticRitualRaiserComponent>(heretic.ChosenRitual.Value);

        Entity<HereticRitualRaiserComponent, HereticRitualComponent> ritEnt = (heretic.ChosenRitual.Value, raiser,
            ritual);

        SetupBlackboard(ritEnt, args.User, mind, ent);

        if (TryDoRitual(ritEnt, args.User))
        {
            if (ritual.PlaySuccessAnimation)
                RitualSuccess(ent, args.User, true);
        }
        else if (TryGetValue(ritEnt, CancelString, out string? cancelStr))
            _popup.PopupClient(cancelStr, ent, args.User);

        raiser.Blackboard.Clear();
        Dirty(ritEnt);
    }

    private void OnExamine(Entity<HereticRitualRuneComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.Examiner, out var h, out _))
            return;

        var name = h.ChosenRitual != null ? Name(h.ChosenRitual.Value) : Loc.GetString("heretic-ritual-none");
        args.PushMarkup(Loc.GetString("heretic-ritualrune-examine", ("rit", name)));
    }

    public void RitualSuccess(EntityUid ent, EntityUid user, bool predicted)
    {
        _audio.PlayPredicted(RitualSuccessSound, ent, predicted ? user : null, AudioParams.Default.WithVolume(-3f));
        var popup = Loc.GetString("heretic-ritual-success");
        _popup.PopupPredicted(popup, ent, predicted ? user : null, Filter.Entities(user), false);
        PredictedSpawnAttachedTo("HereticRuneRitualAnimation", ent.ToCoordinates());
    }

    #endregion
}
