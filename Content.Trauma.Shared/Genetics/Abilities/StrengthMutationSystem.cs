using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed class StrengthMutationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrengthMutationComponent, GetUserMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnGetMeleeDamage(Entity<StrengthMutationComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        args.Damage *= ent.Comp.MeleeModifier;
    }
}
