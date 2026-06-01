// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Trauma.Shared.Vampires.Haemomancer;

namespace Content.Trauma.Shared.Vampires;

public sealed partial class VampireSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, BloodsuckingSuccessEvent>(OnBloodsucking);
        SubscribeLocalEvent<VampireComponent, BloodLeecherAttemptEvent>(OnBloodLeechingAttempt);

        SubscribeLocalEvent<VampireComponent, VampireBloodAlertEvent>(OnAlertClick);
        SubscribeLocalEvent<VampireComponent, GlareAttemptEvent>(OnGlare);
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

    private void OnAlertClick(Entity<VampireComponent> ent, ref VampireBloodAlertEvent args)
    {
        var usable = ent.Comp.UsableBlood;

        _popup.PopupClient($"You have {usable} usable blood", ent.Owner, ent.Owner, PopupType.Large);
    }

    private void OnGlare(Entity<VampireComponent> ent, ref GlareAttemptEvent args)
    {
        args.Cancelled = true;
    }

    #region Public Api

    /// <summary>
    /// Adjusts the <see cref="VampireComponent.UsableBlood"/> and <see cref="VampireComponent.TotalBlood"/> of the vampire
    /// </summary>
    public void AdjustBlood(Entity<VampireComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.UsableBlood += amount;
        ent.Comp.TotalBlood += amount;
        Dirty(ent);

        var ev = new VampireTotalBloodChangedEvent(ent.Comp.TotalBlood);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    /// <summary>
    /// Subtracts an amount from the <see cref="VampireComponent.UsableBlood"/>.
    /// </summary>
    public void SubtractUsableBlood(Entity<VampireComponent?> ent, int amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
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
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return ent.Comp.UsableBlood >= amount;
    }

    /// <summary>
    /// Returns the total blood of the vampire.
    /// </summary>
    public int GetTotalBlood(Entity<VampireComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return 0;

        return ent.Comp.TotalBlood;
    }
    #endregion
}

/// <summary>
/// Raised on the vampire when the total blood increases.
/// </summary>
[ByRefEvent]
public record struct VampireTotalBloodChangedEvent(int Blood);
