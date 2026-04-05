using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Trauma.Common.Plumbing;

public abstract partial class CommonPlumbingSystem : EntitySystem
{
    public abstract void UpdateNodeVisuals(EntityUid uid);
    public abstract bool IsPipeNode<T>(T node);
    public abstract (PipeDirection, AtmosPipeLayer) GetAllDirectionsAndLayers<T>(Entity<TransformComponent> pipe, T node);
}
