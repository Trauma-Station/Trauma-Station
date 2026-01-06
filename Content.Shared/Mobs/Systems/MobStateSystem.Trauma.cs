using Content.Shared.Mobs.Components;

namespace Content.Shared.Mobs.Systems;

/// <summary>
/// Trauma - methods relating to softcrit/hardcrit
/// </summary>
public partial class MobStateSystem
{
    private void CheckActHardcrit(EntityUid target, MobStateComponent component, CancellableEntityEventArgs args)
    {
        switch (component.CurrentState)
        {
            case MobState.Dead:
            case MobState.Critical:
                args.Cancel();
                break;
        }
    }

    /// <summary>
    /// Check if a Mob is specifically softcrit, not hardcrit.
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="component">The MobState component owned by the target</param>
    /// <returns>If the entity is Critical</returns>
    public bool IsSoftCrit(EntityUid target, MobStateComponent? component = null)
    {
        if (!_mobStateQuery.Resolve(target, ref component, false))
            return false;
        return component.CurrentState == MobState.SoftCrit;
    }

    /// <summary>
    /// Check if a Mob is specifically hardcrit, not softcrit.
    /// </summary>
    /// <param name="target">Target Entity</param>
    /// <param name="component">The MobState component owned by the target</param>
    /// <returns>If the entity is Critical</returns>
    public bool IsHardCrit(EntityUid target, MobStateComponent? component = null)
    {
        if (!_mobStateQuery.Resolve(target, ref component, false))
            return false;
        return component.CurrentState == MobState.Critical;
    }
}
