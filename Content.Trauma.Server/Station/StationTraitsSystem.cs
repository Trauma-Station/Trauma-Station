// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Station;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

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

        SubscribeLocalEvent<StationTraitsComponent, MapInitEvent>(OnTraitsInit);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();
    }

    private void OnTraitsInit(Entity<StationTraitsComponent> ent, ref MapInitEvent args)
    {
        var picked = PickTraits(ent);
        foreach (var trait in picked)
        {
            _effects.ApplyEffects(ent, _proto.Index(trait).Effects);
        }

        // TODO: queue sending a station report like 20 seconds into the round
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

    public List<ProtoId<StationTraitPrototype>> PickTraits(Entity<StationTraitsComponent> ent)
    {
        var picked = new List<ProtoId<StationTraitPrototype>>();
        var rolls = ent.Comp.Rolls;
        foreach (var (group, chance) in ent.Comp.Groups)
        {
            PickTraits(picked, group, chance, rolls);
        }
        return picked;
    }

    private void PickTraits(List<ProtoId<StationTraitPrototype>> picked, StationTraitGroup group, float chance, int rolls)
    {
        var pool = new List<StationTraitPrototype>(AllTraits[group]);
        pool.RemoveAll(t => t.AnyConflicting(picked));
        for (int i = 0; i < rolls; i++)
        {
            if (!_random.Prob(chance))
                continue;

            if (pool.Count == 0)
                return; // shouldn't really happen but whatever...

            var trait = _random.PickAndTake(pool);
            picked.Add(trait.ID);
            pool.RemoveAll(t => t.Conflicts.Contains(trait.ID));
        }
    }
}
