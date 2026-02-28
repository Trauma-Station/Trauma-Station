using Content.Goobstation.Common.Religion;
using Content.Shared._DV.CosmicCult.Components;
using Content.Shared._DV.CosmicCult.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Antag;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Stacks;
using Content.Shared._DV.Roles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;


namespace Content.Shared._DV.CosmicCult;

public abstract class SharedCosmicCultSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, ComponentGetStateAttemptEvent>(OnCosmicCultCompGetStateAttempt);
        SubscribeLocalEvent<CosmicCultComponent, ComponentStartup>(DirtyCosmicCultComps);

        SubscribeLocalEvent<CosmicCultExamineComponent, ExaminedEvent>(OnCosmicCultExamined);
        SubscribeLocalEvent<CosmicSubtleMarkComponent, ExaminedEvent>(OnSubtleMarkExamined);
        SubscribeLocalEvent<CosmicShopComponent, LevelUpconfirmedMessage>(OnLevelUpConfirmed);
        SubscribeLocalEvent<CosmicEntropyMoteComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CosmicCultComponent, UserShouldTakeHolyEvent>(OnShouldTakeHoly);
    }

    private void OnShouldTakeHoly(Entity<CosmicCultComponent> ent, ref UserShouldTakeHolyEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.WeakToHoly = true;

        args.ShouldTakeHoly = HasComp<CosmicStarMarkComponent>(ent);
    }

    private void OnUseInHand(Entity<CosmicEntropyMoteComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;
        if (EntityIsCultist(args.User))
        {
            if (!TryComp<CosmicCultComponent>(args.User, out var cultComp)) // Only the cultists can absorb entropy
            {
                _popup.PopupClient(Loc.GetString("cosmic-entropy-interact-noncultist"), args.User, args.User);
                return;
            }
            if (cultComp.EntropyLocked) // Can't absorb any more
            {
                _popup.PopupClient(Loc.GetString("cosmicability-siphon-full"), args.User, args.User);
                return;
            }
            var total = _stack.GetCount(ent.Owner); // Absorb as much as possible
            var absorbed = AddEntropy((args.User, cultComp), total);
            if (TryComp<StackComponent>(ent, out var stackComp)) _stack.SetCount((ent.Owner, stackComp), total - absorbed);

            var ev = new CosmicSiphonIndicatorEvent();
            RaiseLocalEvent(args.User, ev);

            _popup.PopupClient(Loc.GetString(total == absorbed ? "cosmic-entropy-interact-absorb" : "cosmic-entropy-interact-absorb-partial"), args.User, args.User);
        }
        else // Not a part of the cult, destroy the mote
        {
            _audio.PlayPredicted(ent.Comp.ShatterSFX, args.User, args.User);
            _popup.PopupPredicted(
                Loc.GetString("cosmic-entropy-interact-shatter"),
                Loc.GetString("cosmic-entropy-interact-shatter-others", ("user", Identity.Entity(args.User, EntityManager))),
                args.User,
                args.User
            );
            if (_net.IsServer) // Predicted spawn looks bad with animations
                PredictedSpawnAtPosition(ent.Comp.ShatterVFX, Transform(ent).Coordinates);

            PredictedQueueDel(ent);
        }
    }

    private void OnCosmicCultExamined(Entity<CosmicCultExamineComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(EntityIsCultist(args.Examiner) ? ent.Comp.CultistText : ent.Comp.OthersText));
    }

    private void OnSubtleMarkExamined(Entity<CosmicSubtleMarkComponent> ent, ref ExaminedEvent args)
    {
        var ev = new SeeIdentityAttemptEvent();
        RaiseLocalEvent(ent, ev);
        if (ev.TotalCoverage.HasFlag(IdentityBlockerCoverage.EYES)) return;

        args.PushMarkup(Loc.GetString(ent.Comp.ExamineText));
    }

    /// <summary>
    /// Whether the entity is a part of the cosmic cult
    /// </summary>
    /// <param name="includeLesser"> If false, cosmic servants are not considered part of the cult</param>
    public bool EntityIsCultist(EntityUid ent, bool includeLesser = true)
    {
        return HasComp<CosmicCultComponent>(ent)
        || HasComp<CosmicColossusComponent>(ent)
        || HasComp<CosmicLesserCultistComponent>(ent) && includeLesser
        || _mind.TryGetMind(ent, out var mind, out _) && _role.MindHasRole<CosmicCultRoleComponent>(mind);
    }

    public bool EntitySeesCult(EntityUid ent)
    {
        return EntityIsCultist(ent) || HasComp<GhostComponent>(ent);
    }

    /// <summary>
    /// Unlocks given influence for the specified cultist.
    /// </summary>
    /// <param name="ent">The cultist for which to unlock the influence</param>
    /// <param name="influence">The influence to unlock</param>
    /// <param name="force">If true, unlocks the influence even if the cultist didn't reach the required level yet</param>
    public bool UnlockInfluence(Entity<CosmicCultComponent> ent, ProtoId<InfluencePrototype> influence, bool force = false)
    {
        if (!_proto.TryIndex(influence, out var proto) || !force && proto.Tier > ent.Comp.CurrentLevel)
            return false;
        ent.Comp.OwnedInfluences.Remove(influence);
        ent.Comp.UnlockedInfluences.Add(influence);
        Dirty(ent, ent.Comp);
        return true;
    }


    /// <summary>
    /// Determines if a Cosmic Cultist component should be sent to the client.
    /// </summary>
    private void OnCosmicCultCompGetStateAttempt(EntityUid uid, CosmicCultComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(args.Player);
    }

    /// <summary>
    /// The criteria that determine whether a Cult Member component should be sent to a client.
    /// </summary>
    /// <param name="player">The Player the component will be sent to.</param>
    private bool CanGetState(ICommonSession? player)
    {
        //Apparently this can be null in replays so I am just returning true.
        if (player?.AttachedEntity is not { } uid)
            return true;

        if (EntitySeesCult(uid))
            return true;

        return HasComp<ShowAntagIconsComponent>(uid);
    }

    /// <summary>
    /// Dirties all the Cult components so they are sent to clients.
    ///
    /// We need to do this because if a Cult component was not earlier sent to a client and for example the client
    /// becomes a Cult then we need to send all the components to it. To my knowledge there is no way to do this on a
    /// per client basis so we are just dirtying all the components.
    /// </summary>
    private void DirtyCosmicCultComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
    {
        var cosmicCultComps = AllEntityQuery<CosmicCultComponent>();
        while (cosmicCultComps.MoveNext(out var uid, out var comp))
            Dirty(uid, comp);
    }

    public void MalignEcho(Entity<CosmicCultComponent> ent)
    {
        if (ent.Comp.CurrentLevel >= 1)
            PredictedSpawnAtPosition(ent.Comp.CosmicEchoVfx, Transform(ent).Coordinates);
        if (ent.Comp.CurrentLevel >= 3)
        {
            if (HasComp<CosmicStarMarkComponent>(ent)) return; // Doesn't get any more obvious, no need to add even more visuals
            EnsureComp<CosmicMalignEchoComponent>(ent, out var comp);
            comp.ExpireTimer = _timing.CurTime + TimeSpan.FromSeconds(20);
        }
    }

    /// <summary>
    /// Gives a specified amount of entropy to a cosmic cultist. Returns how much entropy was actually added.
    /// </summary>
    public virtual int AddEntropy(Entity<CosmicCultComponent> ent, int amount)
    {
        amount = Math.Min(amount, ent.Comp.EntropyForNextLevel - ent.Comp.TotalEntropy + ent.Comp.EntropyRequirementOffset);
        ent.Comp.TotalEntropy += amount;
        ent.Comp.EntropyBudget += amount;
        TryLevelUp(ent);
        if (ent.Comp.CosmicShopActionEntity is { } shop)
            _ui.SetUiState(shop, CosmicShopKey.Key, new CosmicShopBuiState());
        return amount;
    }

    private void TryLevelUp(Entity<CosmicCultComponent> ent)
    {
        if (ent.Comp.TotalEntropy - ent.Comp.EntropyRequirementOffset < ent.Comp.EntropyForNextLevel) return;
        ent.Comp.EntropyLocked = true; // Enough entropy to level up, so no more entropy until you do.

        if (ent.Comp.CurrentLevel >= ent.Comp.MaxLevel) return; // Max level reached! Can't go any further than that.
        LevelUp(ent);
    }

    public virtual void LevelUp(Entity<CosmicCultComponent> ent)
    {
        ent.Comp.LevelUpAwaitingConfirmation = true;
    }

    public virtual void OnLevelUpConfirmed(Entity<CosmicShopComponent> ent, ref LevelUpconfirmedMessage args)
    {
        if (!TryComp<CosmicCultComponent>(args.Actor, out var cultComp)
            || cultComp.CurrentLevel >= cultComp.MaxLevel
            || cultComp.TotalEntropy - cultComp.EntropyRequirementOffset < cultComp.EntropyForNextLevel) return;
        cultComp.LevelUpAwaitingConfirmation = false;
        cultComp.CurrentLevel++;
        cultComp.EntropyForNextLevel += cultComp.EntropyLevelRequirementIncrease;
        cultComp.EntropyRequirementOffset = cultComp.TotalEntropy;
        Dirty(args.Actor, cultComp);

        foreach (var influenceProto in _proto.EnumeratePrototypes<InfluencePrototype>().Where(influenceProto => influenceProto.Tier == cultComp.CurrentLevel))
            cultComp.UnlockedInfluences.Add(influenceProto.ID);

        switch (cultComp.CurrentLevel)
        {
            case 1:
                cultComp.EntropyLocked = false;
                break;
            case 2:
                EnsureComp<CosmicSubtleMarkComponent>(args.Actor);
                cultComp.EntropyLocked = false;
                break;
            case 3:
                cultComp.MonumentActionEntity = _actions.AddAction(args.Actor, cultComp.MonumentAction);
                break;
        }
    }
}
