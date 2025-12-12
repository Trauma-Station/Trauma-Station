using Content.Shared.Emp;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Stunnable;

namespace Content.Goobstation.Shared.Stunnable;

public sealed class BatongEmpSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunbatonComponent, EmpPulseEvent>(OnEmp);
    }

    private void OnEmp(Entity<StunbatonComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;
        _toggle.TryDeactivate(ent.Owner);
    }
}
