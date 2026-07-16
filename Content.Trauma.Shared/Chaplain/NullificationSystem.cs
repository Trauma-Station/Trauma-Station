// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Chaplain.Components;

namespace Content.Trauma.Shared.Chaplain;

public sealed partial class NullificationSystem : EntitySystem
{
    [Dependency] private EntityQuery<NullificationComponent> _nullificationQuery = default!;

    /// <summary>
    /// Adjusts nullification on the target.
    /// </summary>
    public void AdjustNullification(Entity<NullificationComponent?> ent, int amount)
    {
        if (!_nullificationQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var newAmount = Math.Clamp(ent.Comp.CurrentNullification + amount, 0, ent.Comp.MaxNullification);
        ent.Comp.CurrentNullification = newAmount;
        Dirty(ent);
    }
}
