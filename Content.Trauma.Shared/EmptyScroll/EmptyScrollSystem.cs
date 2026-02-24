// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Shared.EntityTable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Paper;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EmptyScroll;

public sealed class EmptyScrollSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>
    /// Every prayer indexed by the FullPrayer string.
    /// </summary>
    public Dictionary<string, ScrollPrayerPrototype> AllPrayers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmptyScrollComponent, PaperWrittenEvent>(OnWritten);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    private void OnWritten(Entity<EmptyScrollComponent> ent, ref PaperWrittenEvent args)
    {
        RemComp(ent, ent.Comp); // remove it immediately to prevent multiple people trying to write in the same tick

        // if you have a written empty scroll prototype (no user) it spawns items etc on itself.
        var target = args.User ?? ent.Owner;
        var coords = Transform(ent).Coordinates;
        var answered = false;
        if (GetPrayer(args.Content.Trim()) is {} prayer)
        {
            Pray(target, prayer);
            answered = true;
        }

        LocId msg = "empty-scroll-prayer-" + (answered ? "answered" : "failed");
        _popup.PopupCoordinates(Loc.GetString(msg), coords, answered ? PopupType.Large : PopupType.Medium);

        QueueDel(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ScrollPrayerPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        AllPrayers.Clear();
        foreach (var prayer in _proto.EnumeratePrototypes<ScrollPrayerPrototype>())
        {
            foreach (var subject in prayer.Subjects)
            {
                AllPrayers.Add($"O LORD\n{prayer.Verb}\n{subject}", prayer);
            }
        }
    }

    public ScrollPrayerPrototype? GetPrayer(string text)
        => AllPrayers.TryGetValue(text, out var prayer) ? prayer : null;

    public void Pray(EntityUid target, ScrollPrayerPrototype prayer)
    {
        // give items before any effects happen
        if (prayer.Items is {} table)
        {
            var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(target).Id);
            var rand = new System.Random(seed);
            foreach (var id in _entityTable.GetSpawns(table, rand))
            {
                var item = PredictedSpawnNextToOrDrop(id, target);
                _hands.TryPickupAnyHand(target, item);
            }
        }

        // do the effects
        _effects.ApplyEffects(target, prayer.Effects, user: target);
    }
}
