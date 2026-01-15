using Content.Server.NPC.HTN;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;

namespace Content.Trauma.Server.ChangeHTNOnEmag;

public sealed class ChangeHtnOnEmagSystem : EntitySystem
{
    [Dependency] private readonly HTNSystem _htn = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<ChangeHtnOnEmagComponent, GotEmaggedEvent>(OnEmag);
    }

    private void OnEmag(Entity<ChangeHtnOnEmagComponent> ent, ref GotEmaggedEvent args)
    {
        if(HasComp<EmaggedComponent>(ent) || args.Type == EmagType.Access)
            return;

        args.Handled = true;

        EnsureComp<HTNComponent>(ent, out var htn);

        htn.RootTask = ent.Comp.Task;
        _htn.Replan(htn);
    }
}
