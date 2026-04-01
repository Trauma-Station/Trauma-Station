// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Inventory;

namespace Content.Trauma.Common.Wizard;

public sealed class GetSpellInvocationEvent(MagicSchool school, EntityUid performer) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.EYES;

    public MagicSchool School = school;

    public EntityUid Performer = performer;

    public DamageSpecifier ToHeal = new();

    public LocId? Invocation;
}

public sealed class GetMessageColorOverrideEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.EYES;

    public Color? Color = null;
}
