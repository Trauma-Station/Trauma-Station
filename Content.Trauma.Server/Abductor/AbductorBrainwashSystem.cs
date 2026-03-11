using Content.Goobstation.Server.Mindcontrol;
using Content.Goobstation.Shared.Mindcontrol;
using Content.Shared.Mindshield.Components;
using Content.Medical.Shared.Abductor;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Abductor;

public sealed partial class AbductorBrainwashSystem : EntitySystem

{
    [Dependency] private readonly MindcontrolSystem _mindcontrol = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbductorGizmoComponent, BrainwashDoAfterEvent>(OnBrainwashDoAfterEvent);
    }

    private void OnBrainwashDoAfterEvent(Entity<AbductorGizmoComponent> ent, ref BrainwashDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not  {} target)
        return;
        if (HasComp<MindShieldComponent>(target))
        return;

        var comp = EnsureComp<MindcontrolledComponent>(target);
        comp.Master = args.User;
        _mindcontrol.Start(target, comp);
        Timer.Spawn(TimeSpan.FromMinutes(15), () =>
        {
            if (TryComp<MindcontrolledComponent>(target, out var mindcomp))
            RemComp<MindcontrolledComponent>(target);
        });
    }
}
