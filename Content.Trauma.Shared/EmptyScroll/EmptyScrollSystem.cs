// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.EntityTable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Paper;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.EmptyScroll;

public sealed partial class EmptyScrollSystem : EntitySystem
{
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <summary>
    /// Every prayer indexed by the FullPrayer string.
    /// </summary>
    public Dictionary<string, ScrollPrayerPrototype> AllPrayers = new();
    /// <summary>
    /// List of every valid prayer text.
    /// </summary>
    public List<string> AllPrayerTexts = new();

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
            Pray(target, prayer, args.User);
            answered = true;
        }
        else if (_player.LocalEntity == target && _timing.IsFirstTimePredicted)
        {
            var ev = new PrayerFailedEvent();
            RaiseLocalEvent(ref ev);
        }

        LocId msg = "empty-scroll-prayer-" + (answered ? "answered" : "failed");
        _popup.PopupClient(Loc.GetString(msg), coords, target, answered ? PopupType.Large : PopupType.Medium);

        PredictedQueueDel(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ScrollPrayerPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        AllPrayers.Clear();
        AllPrayerTexts.Clear();
        foreach (var prayer in _proto.EnumeratePrototypes<ScrollPrayerPrototype>())
        {
            foreach (var subject in prayer.Subjects)
            {
                var text = $"O LORD\n{prayer.Verb}\n{subject}";
                AllPrayers.Add(text, prayer);
                AllPrayerTexts.Add(text);
            }
        }
    }

    public ScrollPrayerPrototype? GetPrayer(string text)
        => AllPrayers.TryGetValue(text, out var prayer) ? prayer : null;

    public void Pray(EntityUid target, ScrollPrayerPrototype prayer, EntityUid? user = null)
    {
        // give items before any effects happen
        if (prayer.Items is {} table)
        {
            var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(target));
            foreach (var id in _entityTable.GetSpawns(table, rand))
            {
                var item = PredictedSpawnNextToOrDrop(id, target);
                _hands.TryPickupAnyHand(target, item);
            }
        }

        // do the effects
        _effects.ApplyEffects(target, prayer.Effects, user: user);
    }
}

/// <summary>
/// Event broadcast when you don't write a valid prayer and get nothing.
/// </summary>
[ByRefEvent]
public record struct PrayerFailedEvent;
