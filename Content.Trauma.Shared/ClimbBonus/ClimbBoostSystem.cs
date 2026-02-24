using Content.Trauma.Common.ClimbBonus;

namespace Content.Trauma.Shared.ClimbBonus;

/// <summary>
/// This handles the bonus speed when climbing for entities with the ClimbBoostComponent
/// </summary>
public sealed class ClimbBoostSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClimbBoostComponent, ClimbBoostModifierEvent>(OnClimbMod);
    }

    private void OnClimbMod(Entity<ClimbBoostComponent> ent, ref ClimbBoostModifierEvent args)
    {
        if (args.User == args.Target)
            args.Coefficient = ent.Comp.Coefficient;
    }
}
