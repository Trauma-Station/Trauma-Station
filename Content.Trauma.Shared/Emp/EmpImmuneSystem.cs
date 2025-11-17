using Content.Shared.Emp;

namespace Content.Trauma.Shared.Emp;

public sealed class EmpImmuneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpImmuneComponent, EmpAttemptEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<EmpImmuneComponent> ent, ref EmpAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
