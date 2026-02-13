// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Electrocution;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Electrocution;

public sealed class ExplosiveShockSystem : EntitySystem
{
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExplosiveShockComponent, InventoryRelayedEvent<ElectrocutionAttemptEvent>>(OnElectrocuted);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ExplosiveShockIgnitedComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var ignited))
        {
            if (now >= ignited.ExplodeAt)
                TryExplode(uid);
        }
    }

    private void OnElectrocuted(EntityUid uid, ExplosiveShockComponent explosiveShock, InventoryRelayedEvent<ElectrocutionAttemptEvent> args)
    {
        if (!TryComp<ExplosiveComponent>(uid, out var explosive))
            return;

        _popup.PopupEntity(Loc.GetString("explosive-shock-sizzle", ("item", uid)), uid);
        _adminLogger.Add(LogType.Electrocution, $"{ToPrettyString(args.Args.TargetUid):entity} triggered explosive shock item {ToPrettyString(uid):entity}");
        EnsureComp<ExplosiveShockIgnitedComponent>(uid, out var ignited);
        ignited.ExplodeAt = _timing.CurTime + explosiveShock.ExplosionDelay;
    }

    private void TryExplode(EntityUid uid)
    {
        if (Deleted(uid) || !TryComp<ExplosiveComponent>(uid, out var explosive) || !TryComp<ExplosiveShockComponent>(uid, out var explosiveShock))
            return;

        _explosion.TriggerExplosive(uid, explosive);
        if (!TryComp<ClothingComponent>(uid, out var clothing) || clothing.InSlot == null)
            return;

        var target = Transform(uid).ParentUid;

        // gloves go under armor so ignore resistances
        foreach (var part in _part.GetBodyParts(target, BodyPartType.Hand))
            _damageable.ChangeDamage(part, explosiveShock.HandsDamage, true);

        foreach (var part in _part.GetBodyParts(target, BodyPartType.Arm))
            _damageable.ChangeDamage(part, explosiveShock.ArmsDamage, true);

        _stun.TryKnockdown(target, explosiveShock.KnockdownTime, true);
    }
}
