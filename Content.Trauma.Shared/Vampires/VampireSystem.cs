// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vampires;

public sealed class VampireSystem : EntitySystem
{
    #region Public Api

    /// <summary>
    /// Adjusts the usable blood and total blood of the vampire
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
    /// Subtracts an amount from the usable blood.
    /// </summary>
    public void SubtractUsableBlood(Entity<VampireComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.UsableBlood = Math.Clamp(ent.Comp.UsableBlood - amount, 0, ent.Comp.TotalBlood);
        Dirty(ent);
    }
    #endregion
}
