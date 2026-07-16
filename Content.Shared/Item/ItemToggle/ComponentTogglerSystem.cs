// <Trauma>
using Robust.Shared.Timing;
// </Trauma>
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Item.ItemToggle;

/// <summary>
/// Handles <see cref="ComponentTogglerComponent"/> component manipulation.
/// </summary>
public sealed partial class ComponentTogglerSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private IGameTiming _timing = default!;
    // </Trauma>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComponentTogglerComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnToggled(Entity<ComponentTogglerComponent> ent, ref ItemToggledEvent args)
    {
        ToggleComponent(ent, args.Activated);
    }

    // Goobstation - Make this system more flexible
    public void ToggleComponent(EntityUid uid, bool activate)
    {
        // <Trauma>
        if (_timing.ApplyingState)
            return;
        // </Trauma>

        if (!TryComp<ComponentTogglerComponent>(uid, out var component))
            return;


        if (activate)
        {
            var target = component.Parent ? Transform(uid).ParentUid : uid;

            if (TerminatingOrDeleted(target))
                return;

            component.Target = target;

            EntityManager.RemoveComponents(target, component.DeactivateComponents); // Trauma
            EntityManager.AddComponents(target, component.Components);
        }
        else
        {
            if (component.Target == null)
                return;

            if (TerminatingOrDeleted(component.Target.Value))
                return;

            EntityManager.RemoveComponents(component.Target.Value, component.RemoveComponents ?? component.Components);
            EntityManager.AddComponents(component.Target.Value, component.DeactivateComponents); // Trauma
        }
    }
}
