using Content.Server.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Trauma.Common.Silicons.Laws;

namespace Content.Trauma.Server.Silicons.Laws;

public sealed partial class TriggerIonStormSystem : EntitySystem
{
    [Dependency] private IonStormSystem _ionStorm = default!;

    [SubscribeLocalEvent]
    private void OnTriggerIonStorm(
        Entity<SiliconLawBoundComponent> ent, ref TriggerIonStormEvent args)
    {
        if (!TryComp<IonStormTargetComponent>(ent.Owner, out var target))
            return;

        _ionStorm.IonStormTarget((ent.Owner, ent.Comp, target));
    }
}
