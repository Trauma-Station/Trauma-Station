using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;
using System.Numerics;

namespace Content.Shared._VDS.Physics;

/// <summary>
/// Cast rays with better collision detection and the ability to bounce.
/// Uses <see cref="RayCastSystem"/>.
/// </summary>
public sealed partial class ReflectiveRaycastSystem : EntitySystem
{
    [Dependency] private readonly RayCastSystem _rayCast = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    /// <summary>
    /// Automatically cast and iterate through a bouncing ray.
    /// For finer control, use <see cref="CastAndUpdateReflectiveRayStateRef(ref ReflectiveRayState)"/> and iterate
    /// manually. Please consider raising before and after events.
    /// </summary>
    /// <param name="state"><see cref="ReflectiveRayState"/></param>
    /// <param name="maxIterations">Maximum amount of times to bounce.</param>
    /// <param name="useRangeBudget">Whether to stop if the total distance exceeds the state's MaxRange.</param>
    /// <returns>A <see cref="RayResult"/> tuple list of each iteration, containing results for the probe ray and path ray from closest to farthest.</returns>
    [PublicAPI]
    public List<(RayResult probeResults, RayResult pathResults)> CastAndUpdateReflectiveRayStateRef(
        ref ReflectiveRayState state,
        int maxIterations,
        bool useRangeBudget = true)
    {
        var totalRange = 0f;
        var results = new List<(RayResult probeResults, RayResult finalResults)> { };
        for (var i = 0; i < maxIterations; i++)
        {
            results.Add(CastAndUpdateReflectiveRayStateRef(ref state));
            totalRange += state.CurrentSegmentDistance;
            if (useRangeBudget && totalRange >= state.MaxRange)
                break;
        }
        return results;
    }

    /// <summary>
    /// Cast a probing and path ray, updating <paramref name="state"/> depending on context.
    ///
    /// <para>
    /// If the probing ray hits something that matches the <paramref name="state"/> <c>ProbeFilter</c>, it will update the
    /// <paramref name="state"/>, first casting the path ray up to the hit point: <see cref="UpdateStateToPos(ref ReflectiveRayState, in Vector2)"/>,
    /// then updating the <paramref name="state"/> to reflect off of its surface normal: <see cref="UpdateStateReflect(ref ReflectiveRayState)"/>.
    /// Else, we will update the <paramref name="state"/> to continue moving forward: <see cref="UpdateStateForward(ref ReflectiveRayState)"/>.
    /// </para>
    ///
    /// NOTE: Feed the <paramref name="state"/> back into this method in a loop for the ray to bounce around.
    /// </summary>
    /// <param name="state"><see cref="ReflectiveRayState"/></param>
    /// <returns>A <see cref="RayResult"/> tuple, containing results for the probe ray and path ray from closest to farthest.</returns>
    [PublicAPI]
    public (RayResult probeResult, RayResult pathResults) CastAndUpdateReflectiveRayStateRef(ref ReflectiveRayState state)
    {
        /*
            cast a probe ray until it finds the first entity that matches ProbeFilter
            note: _rayCast.CastRayClosest exists and you'd think it would be a better fit for a probe ray, but
            i don't know if i'm just using it wrong or if it's broken cause it seems to clip through walls
            if there is another grid behind that wall...
        */
        var probe = _rayCast.CastRay(
            state.MapId,
            state.CurrentPos,
            state.ProbeTranslation,
            state.ProbeFilter);

        // did we hit something? if so, bounce us.
        if (TryUpdateStateToProbeHit(in probe, ref state))
        {
            var results = _rayCast.CastRay(state.MapId, state.OldPos, state.Translation, state.ResultsFilter);
#if DEBUG
            CastDebugRay(in state);
# endif
            UpdateStateReflect(ref state);
            return (probe, results);
        }
        else // we go forward instead.
        {
            var results = _rayCast.CastRay(state.MapId, state.OldPos, state.Translation, state.ResultsFilter);
            UpdateStateForward(ref state);
#if DEBUG
            CastDebugRay(in state);
# endif
            return (probe, results);
        }
    }


    /// <summary>
    /// Check if our probe actually hit anything.
    /// If so, invoke <see cref="UpdateStateToPos(ref ReflectiveRayState, in Vector2)"/> and
    /// convert the hit local surface normal into world terms.
    ///
    /// </summary>
    /// <param name="state"></param>
    /// <returns>
    /// True if we hit something and updated our <paramref name="state"/>.
    /// <paramref name="state"/> will update via invoking <see cref="UpdateStateToPos(ref ReflectiveRayState, in Vector2)"/>,
    /// alongside updating HitSurfaceNormal.
    ///
    /// False if there is no hit. No state change has happened.
    /// </returns>
    [PublicAPI]
    public bool TryUpdateStateToProbeHit(in RayResult probeResult, ref ReflectiveRayState state)
    {
        if (probeResult.Hit)
        {
            UpdateStateToPos(ref state, probeResult.Results[0].Point);
            state.HitSurfaceNormal = Vector2.TransformNormal(
                probeResult.Results[0].LocalNormal,
                _transformSystem.GetWorldMatrix(probeResult.Results[0].Entity));

            return true;
        }

        return false;
    }

    /// <summary>
    /// Update our <paramref name="state"/> CurrentPos forward by our current Translation,
    /// then gives us a new Translation.
    ///
    /// <list type="bullet">
    /// <listheader>
    /// <description><paramref name="state"/> updates in order:</description>
    /// </listheader>
    /// <item>
    /// <term>OldPos</term>
    /// <description>To our CurrentPos, BEFORE it updates.</description>
    /// </item>
    /// <item>
    /// <term>CurrentPos</term>
    /// <description>Forward using our current Translation.</description>
    /// </item>
    /// <item>
    /// <term>CurrentSegmentDistance</term>
    /// <description>Distance between OldPos and CurrentPos.</description>
    /// </item>
    /// <item>
    /// <term>RemainingDistance</term>
    /// <description>Reduced by CurrentSegmentDistance.</description>
    /// </item>
    /// <item>
    /// <term>Translation</term>
    /// <description>Recalculated with our Direction times our CurrentSegmentDistance.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="state"><see cref="ReflectiveRayState"/></param>
    [PublicAPI]
    public static void UpdateStateForward(ref ReflectiveRayState state)
    {
        state.OldPos = state.CurrentPos;

        // set our new position with our translation
        state.CurrentPos = state.OldPos + state.Translation;

        state.CurrentSegmentDistance = MathF.Max(0.05f, Vector2.Distance(state.OldPos, state.CurrentPos));
        state.RemainingDistance -= state.CurrentSegmentDistance;

        state.Translation = state.Direction * state.CurrentSegmentDistance;
    }

    /// <summary>
    /// Updates our <paramref name="state"/> CurrentPos to <paramref name="worldHitPos"/>,
    /// then gives us a new Translation.
    ///
    /// <list type="bullet">
    /// <listheader>
    /// <description><paramref name="state"/> updates in order:</description>
    /// </listheader>
    /// <item>
    /// <term>OldPos</term>
    /// <description>To our CurrentPos, BEFORE it updates.</description>
    /// </item>
    /// <item>
    /// <term>CurrentPos</term>
    /// <description>To <paramref name="worldHitPos"/>.</description>
    /// </item>
    /// <item>
    /// <term>CurrentSegmentDistance</term>
    /// <description>Distance between OldPos and CurrentPos.</description>
    /// </item>
    /// <item>
    /// <term>RemainingDistance</term>
    /// <description>Reduced by CurrentSegmentDistance</description>
    /// </item>
    /// <item>
    /// <term>Translation</term>
    /// <description>Recalculated with our Direction times our CurrentSegmentDistance.</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="state"><see cref="ReflectiveRayState"/></param>
    /// <param name="worldHitPos">Our destination, in Vector2 world coordinates.</param>
    [PublicAPI]
    public static void UpdateStateToPos(ref ReflectiveRayState state, in Vector2 worldHitPos)
    {
        // update our old position to be the previous one
        state.OldPos = state.CurrentPos;
        // set our new position at the hit entity
        state.CurrentPos = worldHitPos;

        // calculate the distance between our updated points. we use mathf.max so we never get 0 and explode
        state.CurrentSegmentDistance = MathF.Max(0f, Vector2.Distance(state.OldPos, state.CurrentPos));
        state.RemainingDistance -= state.CurrentSegmentDistance;

        state.Translation = state.Direction * state.CurrentSegmentDistance;
    }

    /// <summary>
    /// Reflects the Direction and Translation of our <paramref name="state"/>.
    ///
    /// <list type="bullet">
    /// <listheader>
    /// <description><paramref name="state"/> updates in order:</description>
    /// </listheader>
    /// <item>
    /// <term>CurrentPos</term>
    /// <description>Offsets by HitSurfaceNormal * HitSurfaceOffset. Ensures we're not stuck inside something.</description>
    /// </item>
    /// <item>
    /// <term>Direction</term>
    /// <description>Reflects off of HitSurfaceNormal. Normalizes.</description>
    /// </item>
    /// <item>
    /// <term>Translation</term>
    /// <description>Updates with our new Direction and our unchanged CurrentSegmentDistance</description>
    /// <term>ProbeTranslation</term>
    /// <description>Updates with our new Direction and our unchanged MaxRange</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="state"><see cref="ReflectiveRayState"/></param>
    [PublicAPI]
    public static void UpdateStateReflect(ref ReflectiveRayState state)
    {
        DebugTools.AssertNotNull(state.HitSurfaceNormal, "Can't reflect current raycast without a surface normal!");

        state.CurrentPos += state.HitSurfaceNormal!.Value * state.HitSurfaceOffset;

        // boing
        state.Direction = Vector2.Normalize(Vector2.Reflect(state.Direction, state.HitSurfaceNormal.Value));

        // gas
        state.Translation = state.Direction * state.CurrentSegmentDistance;
        state.ProbeTranslation = state.Direction * state.MaxRange;
    }
}

/// <summary>
/// A reference struct representing the state of our current raycasting.
/// </summary>
/// <param name="probeFilter"><see cref="QueryFilter"/>. Always casted first. Returns the first thing it hits. Used to
/// find how far the <paramref name="pathFilter"/> should go and what we should bounce off of.</param>
/// <param name="pathFilter"><see cref="QueryFilter"/>. Casted second. Returns all filtered things along its path, its
/// path determined by how far <paramref name="probeFilter"/> went until it hit something.</param>
/// <param name="origin">Vector2 world coordinates.</param>
/// <param name="direction">Normalized direction vector.</param>
/// <param name="maxRange">The absolute max range a segment can go. Always used by <paramref name="probeFilter"/>.</param>
/// <param name="mapId">What map are we currently on.</param>
public ref struct ReflectiveRayState(
    QueryFilter probeFilter,
    QueryFilter pathFilter,
    Vector2 origin,
    Vector2 direction,
    float maxRange,
    MapId mapId
    )
{
    public QueryFilter ProbeFilter = probeFilter;
    public QueryFilter ResultsFilter = pathFilter;
    public Vector2 CurrentPos = origin;
    public Vector2 OldPos;
    public Vector2 Direction = direction;
    public float MaxRange = maxRange;
    public float CurrentSegmentDistance = maxRange;
    public float RemainingDistance = maxRange;
    public MapId MapId = mapId;
    public Vector2 Translation = direction * maxRange;
    public Vector2 ProbeTranslation = direction * maxRange;
    public Vector2? HitSurfaceNormal = null;
    public float HitSurfaceOffset = 0.001f;
}
