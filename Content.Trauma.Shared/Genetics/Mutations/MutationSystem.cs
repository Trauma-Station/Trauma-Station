using Content.Shared.Actions.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Forensics.Components;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
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

        SubscribeLocalEvent<MutatableComponent, MapInitEvent>(OnMapInit, after: new[] { typeof(SharedBodySystem) });
        SubscribeLocalEvent<MutatableComponent, PolymorphedEvent>(OnPolymorphed);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();
    }

    private void OnMapInit(Entity<MutatableComponent> ent, ref MapInitEvent args)
    {
        var container = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
        container.OccludesLight = false; // let glowy mutation shine

        if (_net.IsClient) // no rolling stuff
            return;

        // clear is false, don't clear forced mutations in the yml
        Scramble(ent, clear: false, automatic: true);

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

    private void MutationAdded(Entity<MutatableComponent> ent, Entity<MutationComponent> mutation, bool automatic)
    {
        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            _container.Insert(mutation.Owner, container);

        var id = GetID(mutation);
        if (IsForeign(ent, id))
            AddInstability(ent, mutation.Comp.Instability);

        mutation.Comp.Target = ent.Owner;
        Dirty(mutation);

        var ev = new MutationAddedEvent(ent, mutation, automatic);
        RaiseLocalEvent(mutation, ref ev);

        if (automatic)
            return;

        var popup = Loc.GetString(id + "-mutated");
        _popup.PopupEntity(popup, ent, ent, PopupType.MediumCaution);
    }

    private void MutationRemoved(Entity<MutatableComponent> ent, Entity<MutationComponent> mutation, bool automatic)
    {
        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            _container.Remove(mutation.Owner, container);

        var id = GetID(mutation);
        // very important that foreign is checked before removing instability
        // otherwise livrah rat heart incident can happen but for instability instead of damage reduction
        if (IsForeign(ent, id))
            AddInstability(ent, -mutation.Comp.Instability);

        var ev = new MutationRemovedEvent(ent, mutation, automatic);
        RaiseLocalEvent(mutation, ref ev);

        if (!automatic && Loc.TryGetString(id + "-removed", out var popup))
            _popup.PopupEntity(popup, ent, ent, PopupType.MediumCaution);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        RoundData.Clear();
        MutationNumbers.Clear();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            LoadPrototypes();
        if (args.WasModified<MutationRecipePrototype>())
            LoadRecipes();
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

    #region Public API

    /// <summary>
    /// On server, gets the round data for a given mutation or creates it if it doesn't exist.
    /// On client, this always returns null, it can only be gotten through BUI state.
    /// </summary>
    public MutationData? GetRoundData(EntProtoId<MutationComponent>? id)
    {
        if (_net.IsClient || id is null) return null;

        if (RoundData.TryGetValue(id.Value, out var data))
            return data;

        data = new MutationData();
        int number = _random.Next(1, MutationCount);
        while (MutationNumbers.Contains(number));
        {
            // double the number space so it doesnt take a really long time with a lot of mutations trying to roll 1/N chance
            number = _random.Next(1, MutationCount * 2);
        }
        data.Scramble(_random, number);
        RoundData[id.Value] = data;
        return data;
    }

    public MutationData? GetRoundData(EntityUid uid)
        => GetRoundData(Prototype(uid)?.ID);

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
    public bool IsForeign(MutatableComponent comp, EntProtoId<MutationComponent> id)
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
    /// Returns true if an entity can currently mutate.
    /// Corpses cannot mutate because the body has to do work to change every cell.
    /// </summary>
    public bool CanMutate(EntityUid uid)
        => IsMutatable(uid) && !_mob.IsDead(uid);

    public Entity<MutatableComponent>? GetMutatable(EntityUid uid)
        => _mutatableQuery.TryComp(uid, out var comp) && !_mob.IsDead(uid)
           ? (uid, comp)
           : null;

   public EntityUid? GetMutationTarget(EntityUid uid)
       => _query.CompOrNull(uid)?.Target;

    /// <summary>
    /// Tries to add a mutation to an entity, returning true if it succeeded.
    /// Instability increases if the mutation <see cref="IsForeign"/>.
    /// Automatic mutations (from DefaultMutations etc) don't show a popup or polymorph etc.
    /// </summary>
    public bool AddMutation(Entity<MutatableComponent?> ent, EntProtoId<MutationComponent> id, bool automatic = false)
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
        MutationAdded(ent, (uid, _query.Comp(uid)), automatic);
        MutateDna(ent, mutation.Difficulty / 4);
        return true;
    }

    /// <summary>
    /// Add multiple mutations, returning true if any of them succeeded.
    /// </summary>
    public bool AddMutations(Entity<MutatableComponent?> ent, IEnumerable<EntProtoId<MutationComponent>> ids, bool automatic = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (_mob.IsDead(ent))
            return false;

        var added = false;
        foreach (var id in ids)
        {
            added |= AddMutation(ent, id, automatic);
        }
        return added;
    }

    /// <summary>
    /// Tries to activate a dormant mutation, does nothing if the mutation is not present in Dormant.
    /// Won't add instability to the entity.
    /// </summary>
    public bool ActivateMutation(Entity<MutatableComponent?> ent, EntProtoId<MutationComponent> id, bool automatic = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.Dormant.Contains(id) && AddMutation(ent, id, automatic);
    }

    /// <summary>
    /// <see cref="AddMutations"/> for activation.
    /// Returns true if any dormant mutations were added.
    /// </summary>
    public bool ActivateMutations(Entity<MutatableComponent> ent, IEnumerable<EntProtoId<MutationComponent>> ids, bool automatic = false)
    {
        if (_mob.IsDead(ent))
            return false;

        var activated = false;
        foreach (var id in ids)
        {
            activated |= ActivateMutation(ent, id, automatic);
        }

        return activated;
    }

    /// <summary>
    /// Get a mutation by id, or null if it isn't present.
    /// </summary>
    public Entity<MutationComponent>? GetMutation(Entity<MutatableComponent> ent, EntProtoId<MutationComponent> id)
        => ent.Comp.Mutations.TryGetValue(id, out var uid) && _query.TryComp(uid, out var comp)
            ? (uid, comp)
            : null;

    public bool RemoveMutation(Entity<MutatableComponent?> ent, EntProtoId<MutationComponent> id, bool automatic = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (_mob.IsDead(ent))
            return false;

        if (GetMutation(ent, id) is not {} mutation)
            return false; // didn't have it anyways chuddy

        if (_unremoveableQuery.HasComp(mutation))
            return false; // lol no

        foreach (var existing in ent.Comp.Mutations.Values)
        {
            var comp = _query.Comp(existing);
            if (comp.Required.Contains(id))
                return false; // other mutations depend on it
        }

        Log.Debug($"Removed mutation {ToPrettyString(mutation)} from {ToPrettyString(ent)}");
        MutationRemoved(ent, mutation, automatic);
        MutateDna(ent);

        ent.Comp.Mutations.Remove(id);
        Dirty(ent);
        QueueDel(mutation);
        return true;
    }

    /// <summary>
    /// Removes multiple mutations, returning true if any of them succeeded.
    /// </summary>
    public bool RemoveMutations(Entity<MutatableComponent?> ent, IEnumerable<EntProtoId<MutationComponent>> ids, bool automatic = false)
    {
        if (!_mutatableQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (_mob.IsDead(ent))
            return false;

        var added = false;
        foreach (var id in ids)
        {
            added |= RemoveMutation(ent, id, automatic);
        }
        return added;
    }

    /// <summary>
    /// Removes all active and dormant mutations from a mob.
    /// </summary>
    public void ClearMutations(Entity<MutatableComponent> ent, bool automatic = false)
    {
        foreach (var mutation in ent.Comp.Mutations.Values)
        {
            MutationRemoved(ent, mutation, automatic);
            QueueDel(mutation);
        }
        ent.Comp.Mutations.Clear();

        ent.Comp.Dormant.Clear();
        Dirty(ent);
    }

    /// <summary>
    /// Add random default mutations and ensure there's enough dormant mutations.
    /// Optionally removes all active and mutations and dormant mutations beforehand.
    /// </summary>
    public void Scramble(Entity<MutatableComponent> ent, bool clear = true, bool automatic = false)
    {
        if (clear)
            ClearMutations(ent, automatic);

        foreach (var (id, chance) in ent.Comp.DefaultMutations)
        {
            if (_random.Prob(chance))
                AddMutation(ent.AsNullable(), id, automatic: automatic);
        }

        // add enough random dormant mutations so there will be enough sequences.
        while (ent.Comp.Dormant.Count < ent.Comp.MaxDormant)
        {
            ent.Comp.Dormant.Add(_random.Pick(UnlockedMutations));
            Dirty(ent);
        }
    }

    public void TransferMutations(Entity<MutatableComponent> ent, Entity<MutatableComponent> target)
    {
        // remove any mutations it had previously
        ClearMutations(target, automatic: true);

        // replace dormant mutations in the target entity
        foreach (var dormant in ent.Comp.Dormant)
        {
            target.Comp.Dormant.Add(dormant);
        }
        ent.Comp.Dormant.Clear();

        // transfer the mutation entities
        foreach (var (id, mutation) in ent.Comp.Mutations)
        {
            var comp = _query.Comp(mutation);
            MutationRemoved(ent, (mutation, comp), automatic: true);
            MutationAdded(target, (mutation, comp), automatic: true);
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
            QueueDel(ent.Comp.Mutations[id]);
            ent.Comp.Mutations.Remove(id);
        }

        if (_removing.Count > 0)
            Dirty(ent);
        return _removing.Count > 0;
    }

    /// <summary>
    /// Adds instability to an entity.
    /// </summary>
    public void AddInstability(Entity<MutatableComponent> ent, int instability)
    {
        if (instability == 0)
            return;

        ent.Comp.TotalInstability += instability;
        Dirty(ent);
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
