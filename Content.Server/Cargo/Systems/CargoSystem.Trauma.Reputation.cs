using Content.Shared.Cargo.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Tag;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Log;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private void RollMilitaryWants(CargoReputationComponent component)
    {
        var gunPool = new List<string> 
        { 
            "WeaponPistolViper", 
            "WeaponRifleLecter", 
            "WeaponShotgunKammerer",
            "WeaponPistolAnaconda"
        };

        component.MilitaryWants.Clear();
        for (int i = 0; i < 3; i++)
        {
            if (gunPool.Count == 0) break;
            var randomGun = _random.Pick(gunPool);
            component.MilitaryWants.Add(randomGun);
            gunPool.Remove(randomGun);
        }
        
        Logger.InfoS("CargoReputation", $"[SERVER] Rolled unique guns: {string.Join(", ", component.MilitaryWants)}");
    }

    private void ProcessReputationForSoldItem(EntityUid itemUid, EntityUid stationUid)
    {
        CargoReputationComponent? reputationComp = null;
        EntityUid targetUid = EntityUid.Invalid;

        // Прямой поиск первой попавшейся консоли с компонентом
        var query = EntityQueryEnumerator<CargoReputationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            targetUid = uid;
            reputationComp = comp;
            break; // Берем строго одну сущность
        }

        if (reputationComp == null || targetUid == EntityUid.Invalid)
            return;

        // Инициализируем пушки, если список пуст
        if (reputationComp.MilitaryWants.Count == 0)
        {
            RollMilitaryWants(reputationComp);
        }

        var proto = MetaData(itemUid).EntityPrototype?.ID;
        if (proto == null)
            return;

        // 1. Военные
        if (reputationComp.MilitaryWants.Contains(proto))
        {
            reputationComp.MilitaryReputation += 5f;
        }
        else if (HasComp<GunComponent>(itemUid))
        {
            reputationComp.MilitaryReputation += 1f;
        }

        // 2. Медицина
        var tagSystem = EntityManager.System<TagSystem>();
        if (tagSystem.HasTag(itemUid, new ProtoId<TagPrototype>("Medical")) 
            || tagSystem.HasTag(itemUid, new ProtoId<TagPrototype>("Medicine")))
        {
            reputationComp.MedicalReputation += 1f;
        }

        // 3. Сервис
        if (HasComp<EdibleComponent>(itemUid))
        {
            reputationComp.ServiceReputation += 1f;
        }

        // ЖЕСТКОЕ СОХРАНЕНИЕ: Принудительно перезаписываем измененный компонент обратно в энтити сервера
        Dirty(targetUid, reputationComp);
        
        Logger.InfoS("CargoReputation", $"[SERVER SAVE] Item: {proto}. Current Military: {reputationComp.MilitaryReputation}, Medical: {reputationComp.MedicalReputation}, Service: {reputationComp.ServiceReputation}");
    }
}
