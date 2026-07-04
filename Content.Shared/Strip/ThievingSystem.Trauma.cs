using Content.Shared.Inventory;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public sealed partial class ThievingSystem
{
    private void InitializeTrauma()
    {
        SubscribeLocalEvent<ThievingComponent, ThievingStealthCheckEvent>(OnStealthCheck);
        SubscribeLocalEvent<ThievingComponent, InventoryRelayedEvent<ThievingStealthCheckEvent>>((e, c, ev) =>
            OnStealthCheck(e, c, ev.Args));
    }

    private void OnStealthCheck(EntityUid uid, ThievingComponent component, ThievingStealthCheckEvent args)
    {
        args.Stealthy |= component.Stealthy;
    }

    /// <summary>
    /// Returns whether the user currently has stealthy thieving active, whether via a
    /// ThievingComponent on themselves (thief antag) or on equipped gloves.
    /// </summary>
    public bool IsStealthy(EntityUid user)
    {
        var ev = new ThievingStealthCheckEvent();
        RaiseLocalEvent(user, ref ev);
        return ev.Stealthy;
    }
}

/// <summary>
/// Raised on a user to check whether they're currently a stealthy thief, either directly
/// (ex: thief antag ThievingComponent on the mob itself) or via equipped gloves (relayed).
/// </summary>
[ByRefEvent]
public sealed class ThievingStealthCheckEvent : EntityEventArgs, IInventoryRelayEvent
{
    public bool Stealthy;

    public SlotFlags TargetSlots { get; } = SlotFlags.GLOVES;
}
