// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Solstice <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Devil.Objectives.Components;
using Content.Goobstation.Shared.Devil;
using Content.Goobstation.Shared.Devil.Contract;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Wounds;
using Content.Server.Hands.Systems;
using Content.Server.Implants;
using Content.Server.Mind;
using Content.Server.Polymorph.Systems;
using Content.Shared.Body;
using Content.Shared.Paper;
using Content.Shared.Damage.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Goobstation.Server.Devil.Contract;

public sealed partial class DevilContractSystem : SharedDevilContractSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SubdermalImplantSystem _implant = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeSpecialActions();
    }

    #region Helper Events

    protected override void AdvanceObjective(EntityUid devil, int weight)
    {
        if (_mind.TryGetMind(devil, out var mindId, out var mind) &&
            _mind.TryGetObjectiveComp<MeetContractWeightConditionComponent>(mindId, out var objectiveComp, mind))
            objectiveComp.ContractWeight += weight;
    }

    protected override void DoContractEffects(Entity<DevilContractComponent> contract)
    {
        if (!TryComp<PaperComponent>(contract, out var paper))
            return;

        UpdateContractWeight(contract);

        var matches = _clauseRegex.Matches(paper.Content);
        var processedClauses = new HashSet<string>();

        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;

            var targetKey = match.Groups["target"].Value.Trim().ToLowerInvariant().Replace(" ", "");
            var clauseKey = match.Groups["clause"].Value.Trim().ToLowerInvariant().Replace(" ", "");

            var locId = _targetResolvers.Keys.FirstOrDefault(id => Loc.GetString(id).Equals(targetKey, StringComparison.OrdinalIgnoreCase));
            var resolver = _targetResolvers[locId];

            if (resolver(contract.Comp) == null)
            {
                Log.Warning($"Unknown resolver: {resolver(contract.Comp)}");
                continue;
            }

            if (!Proto.TryIndex(clauseKey, out DevilClausePrototype? clause))
            {
                Log.Warning($"Unknown contract clause: {clauseKey}");
                continue;
            }

            // no duplicates
            if (!processedClauses.Add(clauseKey))
            {
                Log.Warning($"Attempted to apply duplicate clause: {clauseKey} on contract {ToPrettyString(contract)}");
                continue;
            }

            if (resolver(contract.Comp) is {} target)
                ApplyEffectToTarget(target, clause, contract);
            else
                Log.Warning($"Invalid target entity from resolver for clause {clauseKey} in contract {ToPrettyString(contract)}");
        }
    }

    private void ApplyEffectToTarget(EntityUid target, DevilClausePrototype clause, Entity<DevilContractComponent>? contract)
    {
        //Log.Debug($"Applying {clause.ID} effect to {ToPrettyString(target)}");

        // TODO: what the fuck is this dogshit, rework to use entity effects
        DoPolymorphs(target, clause);

        RemoveComponents(target, clause);

        AddComponents(target, clause);

        ChangeDamageModifier(target, clause);

        AddImplants(target, clause);

        SpawnItems(target, clause);

        DoSpecialActions(target, contract, clause);
    }

    private void ChangeDamageModifier(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.DamageModifierSet == null)
            return;

        _damageable.SetDamageModifierSetId(target, clause.DamageModifierSet);
        // Log.Debug($"Changed {ToPrettyString(target)} modifier set to {clause.DamageModifierSet}");
    }

    private void RemoveComponents(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.RemovedComponents == null)
            return;

        EntityManager.RemoveComponents(target, clause.RemovedComponents);

        //foreach (var component in clause.RemovedComponents)
        //    Log.Debug($"Removed {component.Value.Component} from {ToPrettyString(target)}");
    }

    private void AddImplants(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.Implants == null)
            return;

        _implant.AddImplants(target, clause.Implants);

        //foreach (var implant in clause.Implants)
        //    Log.Debug($"Added {implant} to {ToPrettyString(target)}");
    }

    private void AddComponents(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.AddedComponents == null)
            return;

        EntityManager.AddComponents(target, clause.AddedComponents, false);

        //foreach (var (name, data) in clause.AddedComponents)
        //    Log.Debug($"Added {data.Component} to {ToPrettyString(target)}");
    }

    private void SpawnItems(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.SpawnedItems == null)
            return;

        foreach (var item in clause.SpawnedItems)
        {
            if (!Proto.TryIndex(item, out _))
                continue;

            var spawnedItem = SpawnNextToOrDrop(item, target);
            _hands.TryPickupAnyHand(target, spawnedItem, false, false, false);

            //Log.Debug($"Spawned {item} for {ToPrettyString(target)}");
        }
    }

    private void DoPolymorphs(EntityUid target, DevilClausePrototype clause)
    {
        if (clause.Polymorph == null)
            return;

        _polymorph.PolymorphEntity(target, clause.Polymorph.Value);
        //Log.Debug($"Polymorphed {ToPrettyString(target)} to {clause.Polymorph} ");
    }

    private void DoSpecialActions(EntityUid target, Entity<DevilContractComponent>? contract, DevilClausePrototype clause)
    {
        if (clause.Event == null)
            return;

        var ev = clause.Event;
        ev.Target = target;

        if (contract is not null)
            ev.Contract = contract;

        // you gotta cast this shit to object, don't ask me vro idk either
        RaiseLocalEvent(target, (object)ev, true);
        //Log.Debug($"Raising event: {(object)ev} on {ToPrettyString(target)}. ");
    }

    public void AddRandomNegativeClause(EntityUid target)
    {
        var negativeClauses = Proto.EnumeratePrototypes<DevilClausePrototype>()
            .Where(c => c.ClauseWeight >= 0)
            .ToList();

        if (negativeClauses.Count == 0)
            return;

        var selectedClause = _random.Pick(negativeClauses);
        ApplyEffectToTarget(target, selectedClause, null);

        Log.Debug($"Selected {selectedClause.ID} effect for {ToPrettyString(target)}");
    }

    public void AddRandomNegativeClauseSlasher(EntityUid target)
    {
        var negativeClauses = Proto.EnumeratePrototypes<DevilClausePrototype>()
            .Where(c => c.ClauseWeight >= 0 && c.ID != "humanity")
            .ToList();

        if (negativeClauses.Count == 0)
            return;

        var selectedClause = _random.Pick(negativeClauses);
        ApplyEffectToTarget(target, selectedClause, null);

        Log.Debug($"Selected {selectedClause.ID} effect for {ToPrettyString(target)}");
    }

    public void AddRandomPositiveClause(EntityUid target)
    {
        var positiveClauses = Proto.EnumeratePrototypes<DevilClausePrototype>()
            .Where(c => c.ClauseWeight <= 0)
            .ToList();

        if (positiveClauses.Count == 0)
            return;

        var selectedClause = _random.Pick(positiveClauses);
        ApplyEffectToTarget(target, selectedClause, null);

        Log.Debug($"Selected {selectedClause.ID} effect for {ToPrettyString(target)}");
    }

    public void AddRandomClause(EntityUid target)
    {
        var clauses = Proto.EnumeratePrototypes<DevilClausePrototype>().ToList();

        if (clauses.Count == 0)
            return;

        var selectedClause = _random.Pick(clauses);
        ApplyEffectToTarget(target, selectedClause, null);

        Log.Debug($"Selected {selectedClause.ID} effect for {ToPrettyString(target)}");
    }

    #endregion
}
