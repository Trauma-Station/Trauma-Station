using Content.Server.NPC.HTN;
using Content.Shared.Emag.Systems;

namespace Content.Trauma.Server.ChangeHTNOnEmag;

public sealed class ChangeHtnOnEmagSystem : EntitySystem
{
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<ChangeHtnOnEmagComponent, GotEmaggedEvent>(OnEmag);
    }

    private void OnEmag(Entity<ChangeHtnOnEmagComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;

        EnsureComp<HTNComponent>(ent, out var htn);

        htn.RootTask = ent.Comp.Task;
        _htn.Replan(htn);
    }
}
