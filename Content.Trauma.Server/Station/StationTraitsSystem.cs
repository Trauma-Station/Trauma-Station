// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Station;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Text;

namespace Content.Trauma.Server.Station;

public sealed class StationTraitsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    /// <summary>
    /// All trait prototypes organized per group.
    /// </summary>
    public Dictionary<StationTraitGroup, List<StationTraitPrototype>> AllTraits = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationTraitsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnBeforePlayerSpawning);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();
    }

    private void OnMapInit(Entity<StationTraitsComponent> ent, ref MapInitEvent args)
    {
        // roll the traits to pick
        var rolls = ent.Comp.Rolls;
        foreach (var (group, chance) in ent.Comp.Groups)
        {
            PickTraits(ent.Comp.Picked, group, chance, rolls);
        }

        // then apply them
        foreach (var id in ent.Comp.Picked)
        {
            var trait = _proto.Index(id);
            Log.Info($"Added station trait {id}");
            try
            {
                if (trait.Effects is {} effects)
                    _effects.ApplyEffects(ent, effects);
            }
            catch (Exception e)
            {
                Log.Error($"Caught exception while applying station trait {id} to {ToPrettyString(ent)}: {e}");
            }

            // and add most traits to the report
            if (trait.Report != null)
                ent.Comp.Reported.Add(id);
        }
    }

    private void OnBeforePlayerSpawning(RulePlayerSpawningEvent args)
    {
        var query = EntityQueryEnumerator<StationTraitsComponent>();
        while (query.MoveNext(out var station, out var comp))
        {
            // never run them multiple times, just incase
            if (comp.RanMapEffects)
                continue;
            comp.RanMapEffects = true;

            // will probably misbehave with multiple stations... oh well
            foreach (var id in comp.Picked)
            {
                var trait = _proto.Index(id);
                if (trait.MapEffects is not {} effects)
                    continue;

                try
                {
                    _effects.ApplyEffects(station, effects);
                }
                catch (Exception e)
                {
                    Log.Error($"Caught exception while applying map effects of station trait {id} to {ToPrettyString(station)}: {e}");
                }
            }
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<StationTraitPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        AllTraits.Clear();
        foreach (var trait in _proto.EnumeratePrototypes<StationTraitPrototype>())
        {
            if (!AllTraits.TryGetValue(trait.Group, out var list))
                AllTraits[trait.Group] = list = new();

            list.Add(trait);
        }
    }

    private void PickTraits(List<ProtoId<StationTraitPrototype>> picked, StationTraitGroup group, float chance, int rolls)
    {
        var all = AllTraits[group];
        var pool = new List<StationTraitPrototype>(all.Count);
        foreach (var trait in all)
        {
            if (_random.Prob(trait.Chance) && !trait.AnyConflicting(picked))
                pool.Add(trait);
        }

        for (int i = 0; i < rolls; i++)
        {
            if (pool.Count == 0)
                return; // shouldn't really happen but whatever...

            if (!_random.Prob(chance))
                continue;

            var trait = _random.PickAndTake(pool);
            picked.Add(trait.ID);
            pool.RemoveAll(t => t.Conflicts.Contains(trait.ID));
        }
    }

    public void AppendReport(StringBuilder sb, EntityUid station)
    {
        if (!TryComp<StationTraitsComponent>(station, out var traits) || traits.Reported.Count == 0)
            return;

        sb.AppendLine("[bold]Identified shift divergencies:[/bold]");
        foreach (var id in traits.Reported)
        {
            var trait = _proto.Index(id);
            if (trait.Report is {} report) // should always be true...
                sb.AppendLine($"[italic]{trait.Name}[/italic] - {report}");
        }
    }
}
