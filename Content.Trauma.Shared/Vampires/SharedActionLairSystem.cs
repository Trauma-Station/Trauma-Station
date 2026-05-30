using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.Vampires.Haemomancer;
using Content.Trauma.Shared.Vampires.Lair;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires;

public abstract partial class SharedActionLairSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedActiveBloodLeecherSystem _leecher = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private EntityQuery<VampireLairComponent> _lairQuery = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodQuery = default!;

    private HashSet<Entity<LightBulbComponent>> _bulbs = new();

    private static readonly EntProtoId LairRune = "VampiricRune";
    private static readonly EntProtoId BeamProto = "BloodBeam";
    private static readonly EntProtoId ActionTeleportLair = "ActionTeleportLair";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionLairComponent, ActionLairEvent>(OnAction);
        SubscribeLocalEvent<ActionLairComponent, VampireLairDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<VampireLairComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<VampireLairComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnAction(Entity<ActionLairComponent> ent, ref ActionLairEvent args)
    {
        var target = args.Target;
        var user = args.Performer;

        if (!_lairQuery.TryComp(target, out var lair))
        {
            _popup.PopupClient("This only works on coffins!", user, user, PopupType.Medium);
            return;
        }

        if (lair.Vampire is not null)
        {
            _popup.PopupClient("This coffin serves another and refuses to bend to your will!", user, user, PopupType.MediumCaution);
            return;
        }

        _popup.PopupClient("You begin making the coffin!", user, user, PopupType.Medium);
        _leecher.CreateBeam(user, target, BeamProto);

        _audio.PlayPredicted(ent.Comp.BeforeCreationSound, target, user);

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user: user,
            delay: ent.Comp.Duration,
            @event: new VampireLairDoAfterEvent(),
            target: target,
            eventTarget: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        _bulbs.Clear();
        _lookup.GetEntitiesInRange(Transform(user).Coordinates, 5f, _bulbs);
        foreach (var bulb in _bulbs)
        {
            GhostBoo(bulb);
        }

        var vampiricRune = PredictedSpawnAtPosition(LairRune, Transform(target).Coordinates);
        ent.Comp.Effect = vampiricRune;
        Dirty(ent);

        if (_bloodQuery.TryComp(user, out var blood))
        {
            _appearance.SetData(vampiricRune, VampiricRuneVisuals.Color, blood.BloodReferenceSolution.GetColor(_proto));
        }

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            PredictedQueueDel(vampiricRune);
    }

    private void OnDoAfter(Entity<ActionLairComponent> ent, ref VampireLairDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Target is not { } target)
        {
            PredictedQueueDel(ent.Comp.Effect);
            return;
        }

        _audio.PlayPredicted(ent.Comp.CreationSound, target, user);

        EntityManager.AddComponents(target, _proto.Index(ent.Comp.LairComponents).Components);
        if (!_lairQuery.TryComp(target, out var lair))
            return;

        _meta.SetEntityName(target, $"The coffin of {Identity.Name(args.User, EntityManager)}");
        _meta.SetEntityDescription(target, "This coffin's owner may not actually have been dear to anyone, or even departed quite yet.");

        lair.Vampire = args.User;
        Dirty(target, lair);

        // Remove the action, only one lair can be created
        if (_action.GetAction(ent.Owner) is not { } action|| action.Comp.AttachedEntity is not { } attached
            || _action.AddAction(attached, ActionTeleportLair) is not { } tpAction)
            return;

        _action.RemoveAction(attached, action.AsNullable());

        // Raised on the new action added, so we can pass the lair to it
        var ev = new VampireLairCreatedEvent(target);
        RaiseLocalEvent(tpAction, ref ev);
    }

    private void OnInserted(Entity<VampireLairComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Vampire is not { } vampire || args.Entity != vampire)
            return;

        _status.TryAddStatusEffect(vampire, ent.Comp.CoffinStatus, out _);
    }

    private void OnRemoved(Entity<VampireLairComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Vampire is not { } vampire || args.Entity != vampire)
            return;

        _status.TryRemoveStatusEffect(vampire, ent.Comp.CoffinStatus);
    }

    /// <summary>
    /// Performs a <see cref="GhostBooEvent"/> on an entity (which exists only in server).
    /// </summary>
    protected virtual void GhostBoo(EntityUid uid) { }
}

[ByRefEvent]
public record struct VampireLairCreatedEvent(EntityUid Lair);
