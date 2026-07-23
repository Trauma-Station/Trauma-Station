using Content.Client.Chemistry.UI;
using Content.Client.Items;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Client.Chemistry.EntitySystems;

public sealed partial class InjectorStatusControlSystem : EntitySystem
{
    [Dependency] private InjectorSystem _injector = default!; // Trauma - replaced _solutionContainers with _injector

    public override void Initialize()
    {
        base.Initialize();
        // Trauma - replace _solutionContainers with _injector
        Subs.ItemStatus<InjectorComponent>(injector => new InjectorStatusControl(injector, _injector, ProtoMan));
    }
}
