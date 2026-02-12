using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Text;
using Content.Shared.Examine;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Body;
using Content.Shared.EntityConditions;
using Content.Shared.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Stacks;
using Content.Shared.Store.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Player;

namespace Content.Shared._Shitcode.Heretic.Rituals;

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
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedEntityConditionsSystem _condition = default!;
    [Dependency] private readonly HereticRitualEffectSystem _effects = default!;

    public SoundSpecifier RitualSuccessSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/castsummon.ogg");

    private EntityQuery<GhoulComponent> _ghoulQuery;
    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<TagComponent> _tagQuery;

    public const string Performer = "Performer";
    public const string Mind = "Mind";
    public const string Platform = "Platform";
    public const string CancelString = "CancelString";

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

    private bool IsSacrificeTarget(Entity<HereticComponent> heretic, EntityUid target)
    {
        return heretic.Comp.SacrificeTargets.Any(x => x.Entity == GetNetEntity(target));
    }

    private void CancelCondition<T>(Entity<HereticRitualComponent> ent,
        ref HereticRitualConditionEvent<T> ev,
        string? cancelString = null)
        where T : BaseRitualCondition<T>
    {
        ev.Result = false;

        if (cancelString != null)
            ent.Comp.Blackboard[CancelString] = cancelString;
    }

    protected bool TryGetValue<T>(Entity<HereticRitualComponent> ent, string key, [NotNullWhen(true)] out T? value)
    {
        if (ent.Comp.Blackboard.TryGetValue(key, out var val))
        {
            value = (T) val;
            return true;
        }

        value = default;
        return false;
    }

    private bool TryDoRitual(Entity<HereticRitualComponent> ent, EntityUid user)
    {
        if (ent.Comp.Limit > 0)
        {
            ent.Comp.LimitedOutput = ent.Comp.LimitedOutput.Where(Exists).ToList();
            if (ent.Comp.LimitedOutput.Count >= ent.Comp.Limit)
            {
                if (ent.Comp.LimitReachedEffects is { } limitReachedEffects)
                    return _effects.TryEffects(ent, limitReachedEffects, ent, user);

                ent.Comp.Blackboard[CancelString] = Loc.GetString("heretic-ritual-fail-limit");
                return false;
            }
        }

        return _effects.TryEffects(ent, ent.Comp.Effects, ent, user);
    }

    private void SetupBlackboard(Entity<HereticRitualComponent> ent,
        EntityUid performer,
        EntityUid mind,
        EntityUid platform)
    {
        ent.Comp.Blackboard.Clear();
        ent.Comp.Blackboard[Performer] = performer;
        ent.Comp.Blackboard[Mind] = mind;
        ent.Comp.Blackboard[Platform] = platform;
        if (ent.Comp.CancelLoc is { } loc)
            ent.Comp.Blackboard[CancelString] = Loc.GetString(loc);
    }

    #endregion

    #region RitualRuneEvents

    private void OnInteract(Entity<HereticRitualRuneComponent> ent, ref InteractHandEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        if (heretic.Rituals.Count == 0)
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

        heretic.ChosenRitual = GetEntity(args.Ritual);
        Dirty(mind, heretic);

        var ritualName = Name(heretic.ChosenRitual.Value);
        _popup.PopupClient(Loc.GetString("heretic-ritual-switch", ("name", ritualName)), user, user);
    }

    private void OnInteractUsing(Entity<HereticRitualRuneComponent> ent, ref InteractUsingEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out var mind))
            return;

        if (!HasComp<MansusGraspComponent>(args.Used))
            return;

        if (!TryComp(heretic.ChosenRitual, out HereticRitualComponent? ritual))
        {
            _popup.PopupClient(Loc.GetString("heretic-ritual-noritual"), args.User, args.User);
            return;
        }

        Entity<HereticRitualComponent> ritEnt = (heretic.ChosenRitual.Value, ritual);

        SetupBlackboard(ritEnt, args.User, mind, ent);

        if (TryDoRitual(ritEnt, args.User))
        {
            if (ritual.PlaySuccessAnimation)
                RitualSuccess(ent, args.User, true);
        }
        else if (TryGetValue(ritEnt, CancelString, out string? cancelStr))
            _popup.PopupClient(cancelStr, ent, args.User);

        ritual.Blackboard.Clear();
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
        PredictedSpawnAtPosition("HereticRuneRitualAnimation", Transform(ent).Coordinates);
    }

    #endregion
}
