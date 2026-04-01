// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Weapons.AmmoSelector;

public abstract partial class CommonSelectableAmmoSystem : EntitySystem
{
    public abstract bool TrySetProto(Entity<AmmoSelectorComponent> ent, ProtoId<SelectableAmmoPrototype> proto);
}
