// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Buckle;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Locks or unlocks the target entity's <see cref="StrapLockComponent"/>.
/// </summary>
public sealed partial class StrapLock : EntityEffectBase<StrapLock>
{
    /// <summary>
    /// Whether to unlock instead of locking.
    /// </summary>
    [DataField]
    public bool Unlock;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // its just used for crucifying idc
}

public sealed class StrapLockEffectSystem : EntityEffectSystem<StrapLockComponent, StrapLock>
{
    [Dependency] private readonly StrapLockSystem _strapLock = default!;

    protected override void Effect(Entity<StrapLockComponent> ent, ref EntityEffectEvent<StrapLock> args)
    {
        if (args.Effect.Unlock)
            _strapLock.UnlockStrap(ent);
        else
            _strapLock.LockStrap(ent);
    }
}
