// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Vampires.Haemomancer;

namespace Content.Trauma.Shared.Vampires;

public sealed class VampireSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, BloodsuckingSuccessEvent>(OnBloodsucking);
        SubscribeLocalEvent<VampireComponent, BloodLeecherAttemptEvent>(OnBloodLeechingAttempt);
    }

    private void OnBloodsucking(Entity<VampireComponent> ent, ref BloodsuckingSuccessEvent args)
    {
        // When bloodsucking succeeds, the vampire gets its usable and total blood increased.
        AdjustBlood(ent.AsNullable(), args.BloodRemoved);
    }

    private void OnBloodLeechingAttempt(Entity<VampireComponent> ent, ref BloodLeecherAttemptEvent args)
    {
        if (HasUsableBlood(ent.AsNullable(), args.BloodRequired))
            return;

        args.Cancelled = true;
    }

    #region Public Api

    /// <summary>
    /// Adjusts the <see cref="VampireComponent.UsableBlood"/> and <see cref="VampireComponent.TotalBlood"/> of the vampire
    /// </summary>
    public void AdjustBlood(Entity<VampireComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.UsableBlood += amount;
        ent.Comp.TotalBlood += amount;
        Dirty(ent);
    }

    /// <summary>
    /// Subtracts an amount from the <see cref="VampireComponent.UsableBlood"/>.
    /// </summary>
    public void SubtractUsableBlood(Entity<VampireComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.UsableBlood = Math.Clamp(ent.Comp.UsableBlood - amount, 0, ent.Comp.TotalBlood);
        Dirty(ent);
    }

    /// <summary>
    /// Checks against an amount, to see if we have enough <see cref="VampireComponent.UsableBlood"/> to surpass it.
    /// </summary>
    /// <returns>True if we have enough <see cref="VampireComponent.UsableBlood"/>, false otherwise</returns>
    public bool HasUsableBlood(Entity<VampireComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (ent.Comp.UsableBlood >= amount)
            return true;

        return false;
    }
    #endregion
}
