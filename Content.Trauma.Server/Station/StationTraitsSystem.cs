// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.EntityEffects;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Shared.Station;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Text;

namespace Content.Trauma.Server.Station;

public sealed class StationTraitsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    /// <summary>
    /// All trait prototypes organized per group.
    /// </summary>
    public Dictionary<StationTraitGroup, List<StationTraitPrototype>> AllTraits = new();

    private delegate EntityEffect[]? GetEffects(StationTraitPrototype trait);

    private bool _enabled;
    private List<ProtoId<StationTraitPrototype>> _forced = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationTraitsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnBeforePlayerSpawning);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();

        Subs.CVar(_cfg, TraumaCVars.StationTraitsEnabled, x => _enabled = x, true);
    }

    private void OnMapInit(Entity<StationTraitsComponent> ent, ref MapInitEvent args)
    {
        if (!_enabled)
            return;

        // add any forced traits and reset them
        foreach (var id in _forced)
        {
            ent.Comp.Picked.Add(id);
        }
        _forced.Clear();

        // roll the traits to pick
        var rolls = ent.Comp.Rolls;
        var players = _player.PlayerCount;
        foreach (var (group, chance) in ent.Comp.Groups)
        {
            PickTraits(ent.Comp.Picked, group, chance, rolls, players);
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

    private void OnRoundStarting(RoundStartingEvent args)
    {
        if (!_enabled)
            return;

        var query = EntityQueryEnumerator<StationTraitsComponent>();
        while (query.MoveNext(out var station, out var comp))
        {
            if (comp.RanStartEffects)
                continue;
            comp.RanStartEffects = true;

            RunEffects((station, comp), trait => trait.StartEffects, "StartEffects");
        }
    }

    private void OnBeforePlayerSpawning(RulePlayerSpawningEvent args)
    {
        if (!_enabled)
            return;

        var query = EntityQueryEnumerator<StationTraitsComponent>();
        while (query.MoveNext(out var station, out var comp))
        {
            if (comp.RanMapEffects)
                continue;
            comp.RanMapEffects = true;

            RunEffects((station, comp), trait => trait.MapEffects, "MapEffects");
        }
    }

    private void RunEffects(Entity<StationTraitsComponent> ent, GetEffects getEffects, string name)
    {
        // will probably misbehave with multiple stations... oh well
        var station = ent.Owner;
        foreach (var id in ent.Comp.Picked)
        {
            var trait = _proto.Index(id);
            if (getEffects(trait) is not {} effects)
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

    private void PickTraits(List<ProtoId<StationTraitPrototype>> picked, StationTraitGroup group, float chance, int rolls, int players)
    {
        var all = AllTraits[group];
        var pool = new List<StationTraitPrototype>(all.Count);
        foreach (var trait in all)
        {
            if (players < trait.MinPlayers ||
                !_random.Prob(trait.Chance) ||
                trait.AnyConflicting(picked) ||
                picked.Contains(trait.ID)) // don't add a rule if it was already forced
                continue;

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

    /// <summary>
    /// Force a trait to be picked for the next round.
    /// This ignores conflicts limits etc.
    /// This will be reset after the round starts.
    /// </summary>
    public bool ForceTrait(string id)
    {
        if (!_enabled || !_proto.HasIndex<StationTraitPrototype>(id))
            return false;

        _forced.Add(id);
        return true;
    }

    /// <summary>
    /// Force all station traits, including conflicting ones, to be picked for the next round...
    /// </summary>
    public void ForceAllTraits()
    {
        foreach (var trait in _proto.EnumeratePrototypes<StationTraitPrototype>())
        {
            ForceTrait(trait.ID);
        }
    }
}
