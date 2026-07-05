using Content.Shared.Inventory;
using Content.Shared.Strip.Components;
using Content.Trauma.Common.Strip;

namespace Content.Shared.Strip;

public sealed partial class ThievingSystem
{
    private void InitializeTrauma()
    {
        Subs.SubscribeWithRelay<ThievingComponent, ThievingStealthCheckEvent>(OnStealthCheck);
    }

    private void OnStealthCheck(EntityUid uid, ThievingComponent component, ref ThievingStealthCheckEvent args)
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
