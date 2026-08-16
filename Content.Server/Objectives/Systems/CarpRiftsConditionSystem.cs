// <Trauma>
using Content.Server.RoundEnd;
// </Trauma>
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class CarpRiftsConditionSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private RoundEndSystem _roundEnd = default!;
    // </Trauma>
    [Dependency] private NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CarpRiftsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, CarpRiftsConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(comp, _number.GetTarget(uid));
    }

    private float GetProgress(CarpRiftsConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (comp.RiftsCharged >= target)
            return 1f;

        return (float) comp.RiftsCharged / (float) target;
    }

    /// <summary>
    /// Increments RiftsCharged, called after a rift fully charges.
    /// </summary>
    public void RiftCharged(EntityUid uid, CarpRiftsConditionComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.RiftsCharged++;

        // <Trauma>
        if (comp.RiftsCharged > 2)
            _roundEnd.RequestRoundEnd(countdownTime: TimeSpan.FromMinutes(5), name: Loc.GetString("dragon-rifts-announcement"));
        // </Trauma>
    }

    /// <summary>
    /// Resets RiftsCharged to 0, called after rifts get destroyed.
    /// </summary>
    public void ResetRifts(EntityUid uid, CarpRiftsConditionComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.RiftsCharged = 0;
    }
}
