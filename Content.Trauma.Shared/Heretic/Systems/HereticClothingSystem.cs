// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed class HereticClothingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly EntityQuery<DamageableComponent> _damageQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticClothingComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<HereticClothingComponent, ToggleClothingAttemptEvent>(OnToggleAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<HereticClothingComponent, ClothingComponent, TransformComponent>();
        while (query.MoveNext(out var comp, out var clothing, out var xform))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;

            if (clothing.InSlotFlag is null or SlotFlags.POCKET)
                continue;

            var parent = xform.ParentUid;

            if (_heretic.IsHereticOrGhoul(parent))
                continue;

            if (!_mob.IsAlive(parent) || !_damageQuery.TryComp(parent, out var dmg))
                continue;

            _dmg.ChangeDamage((parent, dmg), comp.DamageOverTime, ignoreResistances: true, interruptsDoAfters: false, targetPart: TargetBodyPart.Vital);
        }
    }

    private void OnToggleAttempt(Entity<HereticClothingComponent> ent, ref ToggleClothingAttemptEvent args)
    {
        if (_heretic.IsHereticOrGhoul(Transform(ent).ParentUid))
            return;

        args.Cancel();
    }

    private void OnEquip(Entity<HereticClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (_heretic.IsHereticOrGhoul(args.Wearer))
            return;

        _popup.PopupClient(Loc.GetString("heretic-clothing-component-equip", ("item", ent)), args.Wearer, args.Wearer, PopupType.MediumCaution);
    }
}
