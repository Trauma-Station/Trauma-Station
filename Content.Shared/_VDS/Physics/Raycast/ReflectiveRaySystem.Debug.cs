using JetBrains.Annotations;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._VDS.Physics;

public sealed partial class ReflectiveRaycastSystem
{
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;

    /// <summary>
    /// Returns our current state as a string dictionary. Useful for debugging without a debugger, or logging.
    /// </summary>
    /// <param name="state"><see cref="ReflectiveRayState"/></param>
    [PublicAPI]
    public static Dictionary<string, string> GetStateAsStringDictionary(in ReflectiveRayState state)
    {
        return new Dictionary<string, string> {
            {"CurrentPos", $"{state.CurrentPos}"},
            {"OldPos", $"{state.OldPos}"},
            {"Direction", $"{state.Direction}"},
            {"MaxRange", $"{state.MaxRange}"},
            {"CurrentSegmentDistance", $"{state.CurrentSegmentDistance}"},
            {"MapId", $"{state.MapId}"},
            {"Translation", $"{state.Translation}"},
            {"ProbeTranslation", $"{state.ProbeTranslation}"},
            {"HitSurfaceNormal", $"{state.HitSurfaceNormal}"}
        };
    }

    private void CastDebugRay(in ReflectiveRayState state)
    {
        // jank as fuck but whatever
        var debugRay = new CollisionRay(state.OldPos, state.Direction, (int)state.ProbeFilter.LayerBits);
        _physicsSystem.IntersectRay(state.MapId, debugRay, state.CurrentSegmentDistance);
    }
}
