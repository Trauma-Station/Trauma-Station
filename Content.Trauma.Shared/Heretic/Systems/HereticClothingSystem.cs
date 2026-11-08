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
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed partial class HereticClothingSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private InventorySystem _invenory = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageQuery = default!;
    [Dependency] private EntityQuery<HereticBodyComponent> _hereticBodyQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<HereticClothingComponent, ClothingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var clothing, out var xform))
        {
            if (now < comp.NextUpdate)
                continue;

            comp.NextUpdate = now + comp.UpdateDelay;

            if (clothing.InSlotFlag is null or SlotFlags.POCKET || clothing.InSlot is not { } slot)
                continue;

            var parent = xform.ParentUid;

            if (_hereticBodyQuery.HasComp(parent) || _heretic.IsHereticOrGhoul(parent))
                continue;

            if (!_mob.IsAlive(parent) || !_damageQuery.TryComp(parent, out var dmg))
            {
                _invenory.TryUnequip(parent, slot, silent: true, force: true, predicted: false, clothing: clothing);
                continue;
            }

            _dmg.ChangeDamage((parent, dmg), comp.DamageOverTime, ignoreResistances: true, interruptsDoAfters: false, targetPart: TargetBodyPart.Vital);

            if (_random.Prob(comp.DamagePopupProb))
                Popup("heretic-clothing-component-wear", uid, parent);
        }
    }

    [SubscribeLocalEvent]
    private void OnEquip(Entity<HereticClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (_heretic.IsHereticOrGhoul(args.Wearer))
            return;

        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateDelay;
        Popup("heretic-clothing-component-equip", ent, args.Wearer);
    }

    private void Popup(LocId message, EntityUid clothing, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString(message, ("item", clothing)), user, user, PopupType.MediumCaution);
    }
}
