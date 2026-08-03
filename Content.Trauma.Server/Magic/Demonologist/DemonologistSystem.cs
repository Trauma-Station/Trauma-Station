using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Trauma.Shared.Magic.Demonologist.Events;

namespace Content.Trauma.Server.Magic.Demonologist;

public sealed partial class DemonologistSystem : EntitySystem
{
    [Dependency] private FlammableSystem _flammable = default!;

    [SubscribeLocalEvent]
    private void OnCombustion(CombustionSpellEvent args)
    {
        if (!TryComp<FlammableComponent>(args.Target, out var flammable))
            return;

        _flammable.AdjustFireStacks(args.Target, flammable!.MaximumFireStacks, flammable, ignite: true);
        args.Handled = true;
    }
}
