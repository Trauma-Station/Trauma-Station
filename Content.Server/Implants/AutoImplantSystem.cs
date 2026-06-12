// <Trauma>
using Content.Shared.Body.Systems;
// </Trauma>
using Content.Server.Implants.Components;

namespace Content.Server.Implants;

public sealed partial class AutoImplantSystem : EntitySystem
{
    [Dependency] private SubdermalImplantSystem _subdermalImplant = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoImplantComponent, MapInitEvent>(OnMapInit,
            after: [ typeof(SharedBloodstreamSystem) ]); // Trauma - some implants need blood solution to be set up
    }

    private void OnMapInit(EntityUid uid, AutoImplantComponent comp, MapInitEvent args)
    {
        _subdermalImplant.AddImplants(uid, comp.Implants);
    }
}
