// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Standing;

/// <summary>
/// Makes crawling speed depend on held items sizes.
/// </summary>
public sealed class CrawlSpeedSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    /// <summary>
    /// How many hands you need for 100% base crawling speed.
    /// More hands (harmpack) will speed you up, losing hands will slow you down.
    /// </summary>
    public const int ExpectedHandCount = 2;

    /// <summary>
    /// Minimum crawling speed if you lost both of your hands.
    /// </summary>
    public const float MinCrawlSpeed = 0.1f;

    /// <summary>
    /// Cached crawl speed modifiers for each item size.
    /// </summary>
    public readonly Dictionary<ProtoId<ItemSizePrototype>, float> SpeedModifiers = new();

    private EntityQuery<ItemComponent> _itemQuery;

    public override void Initialize()
    {
        base.Initialize();

        _itemQuery = GetEntityQuery<ItemComponent>();

        SubscribeLocalEvent<HandsComponent, KnockedDownRefreshEvent>(OnKnockedDownRefresh);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();
    }

    private void OnKnockedDownRefresh(Entity<HandsComponent> ent, ref KnockedDownRefreshEvent args)
    {
        var totalHands = _hands.GetHandCount(ent.AsNullable());
        if (totalHands == 0)
        {
            args.SpeedModifier *= MinCrawlSpeed; // torsolo can still wiggle around, very slowly
            return; // can't hold anything so don't bother checking
        }

        // first scale by number of hands.
        args.SpeedModifier *= (float) totalHands / ExpectedHandCount;

        // then scale by held item sizes
        foreach (var held in _hands.EnumerateHeld(ent.AsNullable()))
        {
            args.SpeedModifier *= GetSpeedModifier(held);
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ItemSizePrototype>())
            LoadPrototypes();
    }

    public float GetSpeedModifier(EntityUid uid)
    {
        if (!_itemQuery.TryComp(uid, out var item))
            return 0f; // you are carrying someone while crawling??? can't move chuddy

        return SpeedModifiers[item.Size];
    }

    private void LoadPrototypes()
    {
        SpeedModifiers.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<ItemSizePrototype>())
        {
            SpeedModifiers[proto.ID] = proto.CrawlSpeedModifier;
        }
    }
}
