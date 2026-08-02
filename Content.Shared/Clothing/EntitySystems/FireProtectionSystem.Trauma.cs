using Content.Goobstation.Common.Flammability;
using Content.Shared.Armor;
using Content.Shared.Atmos;
using Content.Shared.Body;
using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class FireProtectionSystem : EntitySystem
{
    [Dependency] private EntityQuery<ArmorComponent> _armorQuery = default!;
    [Dependency] private EntityQuery<VeryFlammableComponent> _veryFlammableQuery = default!;

    private void AddCoverage(Entity<FireProtectionComponent> ent, GetFireProtectionEvent args)
    {
        if (!_armorQuery.TryComp(ent, out var armor))
            return;

        foreach (var type in armor.ArmorCoverage)
        {
            foreach (var part in BodySystem.PartTypeOrgans[type])
            {
                args.PartReductions[part] = args.PartReductions.GetValueOrDefault(part) + ent.Comp.Reduction;
            }
        }
    }
}
