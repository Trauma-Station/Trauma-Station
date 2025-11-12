using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed class ArmorMutationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorMutationComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<ArmorMutationComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.Modifiers);
    }
}
