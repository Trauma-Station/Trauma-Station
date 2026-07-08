using Content.Trauma.Common.Strip;

namespace Content.Shared.Strip;

public sealed partial class ThievingSystem
{
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
