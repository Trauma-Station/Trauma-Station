// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Lets players start new forged items from ingots using a radial menu BUI.
/// </summary>
public sealed class AnvilSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly ForgingSystem _forging = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMetalSystem _metal = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private HashSet<Entity<MetalIngotComponent>> _ingots = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ForgingAnvilComponent>(AnvilUiKey.Key, subs =>
        {
            subs.Event<AnvilStartItemMessage>(OnStartItem);
        });
    }

    private void OnStartItem(Entity<ForgingAnvilComponent> ent, ref AnvilStartItemMessage args)
    {
        if (!_proto.TryIndex(args.Metal, out var metal) || !_proto.TryIndex(args.Item, out var item))
            return;

        var user = args.Actor;
        var coords = FindIngots(ent, args.Metal);
        if (_ingots.Count < item.Cost)
        {
            var missing = item.Cost - _ingots.Count;
            _popup.PopupClient($"You are missing {missing} more hot {metal.Name} ingots!",
                ent, user, PopupType.MediumCaution);
            return;
        }

        // theres enough ingots, delete the used ingots
        var deleted = 0;
        foreach (var ingot in _ingots)
        {
            PredictedDel(ingot.Owner);
            if (++deleted == item.Cost)
                break;
        }

        // then create the unfinished item
        var uid = _forging.SpawnUnfinished(coords, args.Metal, args.Item);
        _popup.PopupClient($"You get ready to work on your {Name(uid)}",
            ent, user, PopupType.Medium);
        _audio.PlayPredicted(ent.Comp.StartSound, ent, user);

        _adminLog.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user):player} created {ToPrettyString(uid):item} on anvil {ToPrettyString(ent):used}");
    }

    private EntityCoordinates FindIngots(Entity<ForgingAnvilComponent> ent, [ForbidLiteral] ProtoId<MetalPrototype> metal)
    {
        var coords = Transform(ent).Coordinates;
        var range = ent.Comp.IngotRange;
        var flags = LookupFlags.Uncontained;
        _ingots.Clear();
        _lookup.GetEntitiesInRange(coords, range, _ingots, flags);
        _ingots.RemoveWhere(uid => _metal.GetMetalOrThrow(uid) != metal || !_metal.IsWorkable(uid));
        return coords;
    }
}
