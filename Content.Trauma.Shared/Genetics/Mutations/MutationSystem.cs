// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Forensics.Components;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Text;

namespace Content.Trauma.Shared.Genetics.Mutations;

public sealed partial class MutationSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    /// <summary>
    /// All mutation prototypes and their respective <see cref="MutationComponent"/>.
    /// </summary>
    public Dictionary<EntProtoId<MutationComponent>, MutationComponent> AllMutations = new();

    /// <summary>
    /// How many mutation prototypes there are in total.
    /// </summary>
    public int MutationCount;

    /// <summary>
    /// All mutation ids which don't have <c>locked: true</c> and have no mutation recipe.
    /// </summary>
    public HashSet<EntProtoId<MutationComponent>> UnlockedMutations = new();

    /// <summary>
    /// Per-round data for each mutation, e.g. its bases.
    /// Server only as clients knowing every mutation would be silly.
    /// </summary>
    /// <remarks>
    /// Round entities WYCI
    /// </remarks>
    public Dictionary<EntProtoId<MutationComponent>, MutationData> RoundData = new();
    private HashSet<int> MutationNumbers = new();

    private static readonly ProtoId<DamageGroupPrototype> Genetic = "Genetic";

    private List<EntProtoId<MutationComponent>> _removing = new();

    private EntityQuery<ActionComponent> _actionQuery;
    private EntityQuery<DamageableComponent> _damageableQuery;
    private EntityQuery<DnaComponent> _dnaQuery;
    private EntityQuery<MutatableComponent> _mutatableQuery;
    private EntityQuery<MutationComponent> _query;
    private EntityQuery<UnremoveableComponent> _unremoveableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _actionQuery = GetEntityQuery<ActionComponent>();
        _damageableQuery = GetEntityQuery<DamageableComponent>();
        _dnaQuery = GetEntityQuery<DnaComponent>();
        _mutatableQuery = GetEntityQuery<MutatableComponent>();
        _query = GetEntityQuery<MutationComponent>();
        _unremoveableQuery = GetEntityQuery<UnremoveableComponent>();

        SubscribeLocalEvent<MutatableComponent, MapInitEvent>(OnMapInit, after: new[] { typeof(BodySystem) });
        SubscribeLocalEvent<MutatableComponent, PolymorphedEvent>(OnPolymorphed);
        SubscribeLocalEvent<MutatableComponent, DnaScrambledEvent>(OnDnaScrambled);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadRecipes();
        LoadPrototypes();
    }

    private void OnMapInit(Entity<MutatableComponent> ent, ref MapInitEvent args)
    {
        var container = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
        container.OccludesLight = false; // let glowy mutation shine

        if (_net.IsClient) // no rolling stuff
            return;

        // clear is false, don't clear forced mutations in the yml or from previous polymorph body
        Scramble(ent, clear: false, automatic: true);

        if (ent.Comp.Mutations.Count == 0)
        {
            foreach (var (id, chance) in ent.Comp.DefaultMutations)
            {
                if (_random.Prob(chance))
                    AddMutation(ent.AsNullable(), id, automatic: true, predicted: false);
            }
        }

        RemoveConflictingMutations(ent);
    }

    private void OnPolymorphed(Entity<MutatableComponent> ent, ref PolymorphedEvent args)
    {
        var target = args.NewEntity;
        if (ent.Owner != args.OldEntity || !_mutatableQuery.TryComp(target, out var comp))
            return;

        var dna = GetDna(ent);
        TransferMutations(ent, (target, comp));
        if (dna is {} oldDna)
            SetDna(target, oldDna); // don't change dna by reapplying mutations
    }

    private void OnDnaScrambled(Entity<MutatableComponent> ent, ref DnaScrambledEvent args)
    {
        Scramble(ent, predicted: false); // currently it's only raised on server
    }

    private void MutationAdded(Entity<MutatableComponent> ent, Entity<MutationComponent> mutation, EntityUid? user, bool automatic, bool predicted)
    {
        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            _container.Insert(mutation.Owner, container);

        var id = GetID(mutation);
        if (IsForeign(ent, id))
            AddInstability(ent, mutation.Comp.Instability, automatic: automatic, predicted: predicted);

        mutation.Comp.Target = ent.Owner;
        Dirty(mutation);

        var ev = new MutationAddedEvent(ent, mutation, id, user, automatic, predicted);
        RaiseLocalEvent(mutation, ref ev);
        RaiseLocalEvent(ent, ref ev);

        if (automatic)
            return;

        var popup = Loc.GetString(id + "-mutated");
        if (predicted)
            _popup.PopupClient(popup, ent, ent, PopupType.MediumCaution);
        else
            _popup.PopupEntity(popup, ent, ent, PopupType.MediumCaution);
    }

    private void MutationRemoved(Entity<MutatableComponent> ent, Entity<MutationComponent> mutation, EntityUid? user, bool automatic, bool predicted)
    {
        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            _container.Remove(mutation.Owner, container);

        var id = GetID(mutation);
        // very important that foreign is checked before removing instability
        // otherwise livrah rat heart incident can happen but for instability instead of damage reduction
        if (IsForeign(ent, id))
            AddInstability(ent, -mutation.Comp.Instability, automatic: automatic, predicted: predicted);

        var ev = new MutationRemovedEvent(ent, mutation, id, user, automatic, predicted);
        RaiseLocalEvent(mutation, ref ev);
        RaiseLocalEvent(ent, ref ev);

        if (automatic || !Loc.TryGetString(id + "-removed", out var popup))
            return;

        if (predicted)
            _popup.PopupClient(popup, ent, ent, PopupType.MediumCaution);
        else
            _popup.PopupEntity(popup, ent, ent, PopupType.MediumCaution);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        RoundData.Clear();
        MutationNumbers.Clear();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<MutationRecipePrototype>())
            LoadRecipes();
        if (args.WasModified<EntityPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        MutationCount = 0;
        AllMutations.Clear();
        UnlockedMutations.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (!proto.TryGetComponent<MutationComponent>(out var comp, Factory))
                continue;

            MutationCount++;
            AllMutations[proto.ID] = comp;
            if (!comp.Locked && !HasRecipe(proto.ID))
                UnlockedMutations.Add(proto.ID);
        }
    }

    private LocId? InstabilityPopup(int instability)
        => instability switch
        {
            // 0-10
            < 10 => null,
            // 10-30
            < 30 => "genetics-instability-warning-shiver",
            // 30-40
            < 40 => "genetics-instability-warning-cold",
            // 40-60
            < 60 => "genetics-instability-warning-sick",
            // 60-80
            < 80 => "genetics-instability-warning-skin-moving",
            // 80-100
            < 100 => "genetics-instability-warning-cells-burning",
            // 100+
            _ => "genetics-instability-warning-dna-exploding"
        };

    #region Public API

    /// <summary>
    /// On server, gets the round data for a given mutation or creates it if it doesn't exist.
    /// On client, this always returns null, it can only be gotten through BUI state.
    /// </summary>
    public MutationData? GetRoundData([ForbidLiteral] EntProtoId<MutationComponent>? id)
    {
        if (_net.IsClient || id is null) return null;

        if (RoundData.TryGetValue(id.Value, out var data))
            return data;

        data = new MutationData();
        int number = _random.Next(1, MutationCount);
        while (MutationNumbers.Contains(number))
        {
            // double the number space so it doesnt take a really long time with a lot of mutations trying to roll 1/N chance
            number = _random.Next(1, MutationCount * 2);
        }
        data.Scramble(_random, number);
        RoundData[id.Value] = data;
        MutationNumbers.Add(number);
        return data;
    }

    public MutationData? GetRoundData(EntityUid uid)
        => GetRoundData(GetID(uid));

    /// <summary>
    /// Gets the ID of a mutation, or throws if it isn't valid.
    /// </summary>
    public EntProtoId<MutationComponent> GetID(EntityUid mutation)
    {
        DebugTools.Assert(_query.HasComp(mutation), $"GetID called with non-mutation entity {ToPrettyString(mutation)}");
        if (Prototype(mutation)?.ID is not {} id)
            throw new InvalidOperationException($"GetID called with non-prototyped entity {ToPrettyString(mutation)}");
        // it's assumed that if the entity has the component the prototype also has it.
        return id;
    }

    /// <summary>
    /// Returns true if a mutation is foreign to an entity, i.e. not present in Dormant.
    /// </summary>
    public bool IsForeign(MutatableComponent comp, [ForbidLiteral] EntProtoId<MutationComponent> id)
        => !comp.Dormant.Contains(id);

    /// <summary>
    /// Get the total instability of a mutatable entity.
    /// Returns 0 if the entity is not mutatable.
    /// </summary>
    public int GetInstability(EntityUid uid)
        => _mutatableQuery.CompOrNull(uid)?.TotalInstability ?? 0;

    /// <summary>
    /// Returns true if an entity has <see cref="MutatableComponent"/>.
    /// </summary>
    public bool IsMutatable(EntityUid uid) => _mutatableQuery.HasComp(uid);

    /// <summary>
    /// Returns true if an entity has a specific mutation active.
    /// </summary>
    public bool HasMutation(Entity<MutatableComponent?> ent, [ForbidLiteral] EntProtoId<MutationComponent> id)
        => _mutatableQuery.Resolve(ent, ref ent.Comp) && ent.Comp.Mutations.ContainsKey(id);

    /// <summary>
    /// Returns true if an entity can currently mutate.
    /// Corpses cannot mutate because the body has to do work to change every cell.
    /// </summary>
    public bool CanMutate(EntityUid uid)
        => IsMutatable(uid) && !_mob.IsDead(uid);

    public Entity<MutatableComponent>? GetMutatable(EntityUid uid, bool force = true)
        => _mutatableQuery.TryComp(uid, out var comp) && (force || !_mob.IsDead(uid))
           ? (uid, comp)
           : null;

   public EntityUid? GetMutationTarget(EntityUid uid)
       => _query.CompOrNull(uid)?.Target;

    /// <summary>
    /// Tries to add a mutation to an entity, returning true if it succeeded.
    /// Instability increases if the mutation <see cref="IsForeign"/>.
    /// Automatic mutations (from DefaultMutations etc) don't show a popup or polymorph etc.
    /// </summary>
    public bool AddMutation(Entity<MutatableComponent?> ent, [ForbidLiteral] EntProtoId<MutationComponent> id, EntityUid? user = null, bool automatic = false, bool predicted = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (_mob.IsDead(ent))
            return false;

        if (ent.Comp.Mutations.ContainsKey(id))
            return false; // already have it chuddy

        if (!AllMutations.TryGetValue(id, out var mutation))
            return false; // doesn't exist

        foreach (var good in mutation.Required)
        {
            if (!ent.Comp.Mutations.ContainsKey(good))
                return false; // required mutation missing
        }

        foreach (var bad in mutation.Conflicts)
        {
            if (ent.Comp.Mutations.ContainsKey(bad))
                return false; // conflicting mutation found
        }

        if (!TrySpawnInContainer(id, ent, ent.Comp.ContainerId, out var mutEnt))
            return false; // inserting failed

        var uid = mutEnt.Value;
        Log.Debug($"Added mutation {ToPrettyString(uid)} to {ToPrettyString(ent)}");
        ent.Comp.Mutations[id] = uid;
        Dirty(ent);
        MutationAdded((ent, ent.Comp), (uid, _query.Comp(uid)), user, automatic, predicted);
        MutateDna(ent, mutation.Difficulty / 4);
        return true;
    }

    /// <summary>
    /// Add multiple mutations, returning true if any of them succeeded.
    /// </summary>
    public bool AddMutations(Entity<MutatableComponent?> ent, [ForbidLiteral] IEnumerable<EntProtoId<MutationComponent>> ids, EntityUid? user = null, bool automatic = false, bool predicted = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (_mob.IsDead(ent))
            return false;

        var added = false;
        foreach (var id in ids)
        {
            added |= AddMutation(ent, id, user, automatic, predicted);
        }
        return added;
    }

    /// <summary>
    /// Tries to activate a dormant mutation, does nothing if the mutation is not present in Dormant.
    /// Won't add instability to the entity.
    /// </summary>
    public bool ActivateMutation(Entity<MutatableComponent?> ent, [ForbidLiteral] EntProtoId<MutationComponent> id, EntityUid? user = null, bool automatic = false, bool predicted = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.Dormant.Contains(id) && AddMutation(ent, id, user, automatic, predicted);
    }

    /// <summary>
    /// <see cref="AddMutations"/> for activation.
    /// Returns true if any dormant mutations were added.
    /// </summary>
    public bool ActivateMutations(Entity<MutatableComponent> ent, [ForbidLiteral] IEnumerable<EntProtoId<MutationComponent>> ids, EntityUid? user = null, bool automatic = false, bool predicted = false)
    {
        if (_mob.IsDead(ent))
            return false;

        var activated = false;
        var target = ent.AsNullable();
        foreach (var id in ids)
        {
            activated |= ActivateMutation(target, id, user, automatic, predicted);
        }

        return activated;
    }

    /// <summary>
    /// Get a mutation by id, or null if it isn't present.
    /// </summary>
    public Entity<MutationComponent>? GetMutation(Entity<MutatableComponent> ent, [ForbidLiteral] EntProtoId<MutationComponent> id)
        => ent.Comp.Mutations.TryGetValue(id, out var uid) && _query.TryComp(uid, out var comp)
            ? (uid, comp)
            : null;

    public bool RemoveMutation(Entity<MutatableComponent?> ent, [ForbidLiteral] EntProtoId<MutationComponent> id, EntityUid? user = null, bool automatic = false, bool predicted = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (_mob.IsDead(ent))
            return false;

        if (GetMutation((ent, ent.Comp), id) is not {} mutation)
            return false; // didn't have it anyways chuddy

        if (_unremoveableQuery.HasComp(mutation))
            return false; // lol no

        foreach (var existing in ent.Comp.Mutations.Values)
        {
            var comp = _query.Comp(existing);
            if (comp.Required.Contains(id))
                return false; // other mutations depend on it
        }

        // this is done before the events are raised so monkified is removed from the original body properly
        ent.Comp.Mutations.Remove(id);
        Dirty(ent);

        Log.Debug($"Removed mutation {ToPrettyString(mutation)} from {ToPrettyString(ent)}");
        MutationRemoved((ent, ent.Comp), mutation, user, automatic, predicted);
        MutateDna(ent);

        PredictedQueueDel(mutation);
        return true;
    }

    /// <summary>
    /// Removes multiple mutations, returning true if any of them succeeded.
    /// </summary>
    public bool RemoveMutations(Entity<MutatableComponent?> ent, [ForbidLiteral] IEnumerable<EntProtoId<MutationComponent>> ids, EntityUid? user = null, bool automatic = false, bool predicted = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (_mob.IsDead(ent))
            return false;

        var added = false;
        foreach (var id in ids)
        {
            added |= RemoveMutation(ent, id, user, automatic, predicted);
        }
        return added;
    }

    /// <summary>
    /// Removes all active and dormant mutations from a mob.
    /// </summary>
    public void ClearMutations(Entity<MutatableComponent?> ent, EntityUid? user = null, bool automatic = false, bool predicted = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return;

        foreach (var mutation in ent.Comp.Mutations.Values)
        {
            if (_query.TryComp(mutation, out var mutationComp))
                MutationRemoved((ent, ent.Comp), (mutation, mutationComp), user, automatic, predicted);
            PredictedQueueDel(mutation);
        }
        ent.Comp.Mutations.Clear();

        ent.Comp.Dormant.Clear();
        Dirty(ent);
    }

    /// <summary>
    /// Add random dormant mutations.
    /// Optionally removes all active and mutations and dormant mutations beforehand.
    /// Predicted only applies to clearing, the RNG is not predicted here
    /// as mispredicts from tick timing or something might be disastrous.
    /// </summary>
    public void Scramble(Entity<MutatableComponent> ent, bool clear = true, EntityUid? user = null, bool automatic = false, bool predicted = false)
    {
        if (clear)
            ClearMutations(ent.AsNullable(), user, automatic, predicted);

        // don't scramble existing mutations
        if (ent.Comp.Mutations.Count > 0)
            return;

        // add enough random dormant mutations so there will be enough sequences.
        while (ent.Comp.Dormant.Count < ent.Comp.MaxDormant)
        {
            var picked = _random.Pick(UnlockedMutations);
            if (!ent.Comp.Dormant.Contains(picked))
                ent.Comp.Dormant.Add(picked);
        }

        // don't have dormant mutation as first item
        _random.Shuffle(ent.Comp.Dormant);
        Dirty(ent);

        MutateDna(ent, rolls: 16);
        RemComp<ScannedGenomeComponent>(ent); // have to rescan it now chud
    }

    public void TransferMutations(Entity<MutatableComponent> ent, Entity<MutatableComponent> target, EntityUid? user = null, bool predicted = false)
    {
        // remove any mutations it had previously
        ClearMutations(target.AsNullable(), user, automatic: true, predicted: predicted);

        // replace dormant mutations in the target entity
        foreach (var dormant in ent.Comp.Dormant)
        {
            target.Comp.Dormant.Add(dormant);
        }
        ent.Comp.Dormant.Clear();

        // transfer the mutation entities
        Log.Debug($"Transferring {ent.Comp.Mutations.Count} mutations from {ToPrettyString(ent)} to {ToPrettyString(target)}");
        foreach (var (id, mutation) in ent.Comp.Mutations)
        {
            Log.Debug($"- {ToPrettyString(mutation)}");
            var comp = _query.Comp(mutation);
            MutationRemoved(ent, (mutation, comp), user, automatic: true, predicted: predicted);
            MutationAdded(target, (mutation, comp), user, automatic: true, predicted: predicted);
            target.Comp.Mutations[id] = mutation;
        }
        ent.Comp.Mutations.Clear();

        Dirty(ent);
        Dirty(target);
    }

    /// <summary>
    /// Randomizes <c>rolls</c> letters of the entity's forensics DNA.
    /// </summary>
    public void MutateDna(EntityUid uid, int rolls = 4)
    {
        if (_net.IsClient || !_dnaQuery.TryComp(uid, out var comp) || comp.DNA is not {} dna)
            return;

        var builder = new StringBuilder(dna);
        var max = dna.Length;
        for (int i = 0; i < rolls; i++)
        {
            var n = _random.Next(0, max);
            builder[n] = _random.Pick(MutationData.AGCT);
        }

        comp.DNA = builder.ToString();
        Dirty(uid, comp);
    }

    public string? GetDna(EntityUid uid)
        => _dnaQuery.CompOrNull(uid)?.DNA;

    public void SetDna(EntityUid uid, string dna)
    {
        if (!_dnaQuery.TryComp(uid, out var comp))
            return;

        comp.DNA = dna;
        Dirty(uid, comp);
    }

    /// <summary>
    /// Gets the total genetic damage of a mob, or null if it isn't damageable.
    /// </summary>
    public int? GetGeneticDamage(EntityUid mob)
    {
        if (!_damageableQuery.TryComp(mob, out var comp))
            return null;

        if (!comp.DamagePerGroup.TryGetValue(Genetic, out var damage))
            return 0;

        return (int) damage;
    }

    /// <summary>
    /// Removes any mutations that conflict with others on the entity.
    /// Required mutations are ignored though, so you can write some cool stuff in YML.
    /// </summary>
    public bool RemoveConflictingMutations(Entity<MutatableComponent> ent)
    {
        _removing.Clear();
        foreach (var (id, uid) in ent.Comp.Mutations)
        {
            if (!_query.TryComp(uid, out var comp))
            {
                Log.Error($"{ToPrettyString(ent)} mutation {ToPrettyString(uid)} for {id} was invalid, removing it.");
                _removing.Add(id);
                continue;
            }

            foreach (var bad in comp.Conflicts)
            {
                if (!ent.Comp.Mutations.ContainsKey(bad))
                    continue;

                Log.Error($"{ToPrettyString(ent)} had conflicting mutations {id} and {bad}, removing the former.");
                _removing.Add(id);
                break;
            }
        }

        foreach (var id in _removing)
        {
            PredictedQueueDel(ent.Comp.Mutations[id]);
            ent.Comp.Mutations.Remove(id);
        }

        if (_removing.Count > 0)
            Dirty(ent);
        return _removing.Count > 0;
    }

    /// <summary>
    /// Adds instability to an entity.
    /// Automatic and predicted control the popup logic.
    /// </summary>
    public void AddInstability(Entity<MutatableComponent> ent, int instability, bool automatic, bool predicted)
    {
        if (instability == 0)
            return;

        ent.Comp.TotalInstability += instability;
        Dirty(ent);

        if (!automatic && InstabilityPopup(ent.Comp.TotalInstability) is {} key)
        {
            var msg = Loc.GetString(key);
            if (predicted)
                _popup.PopupClient(msg, ent, ent);
            else
                _popup.PopupEntity(msg, ent, ent);
        }

        if (ent.Comp.TotalInstability >= ent.Comp.MaxInstability)
            _status.TrySetStatusEffectDuration(ent.Owner, ent.Comp.MeltingEffect, ent.Comp.MeltDuration);
        else
            _status.TryRemoveStatusEffect(ent.Owner, ent.Comp.MeltingEffect);
    }

    /// <summary>
    /// Helper for abilities to get the mutation from their action.
    /// </summary>
    public Entity<MutationComponent>? GetActionMutation(EntityUid uid)
    {
        if (_actionQuery.CompOrNull(uid)?.Container is not {} mutation)
            return null;

        if (!_query.TryComp(mutation, out var comp))
            return null;

        return (mutation, comp);
    }

    #endregion
}
