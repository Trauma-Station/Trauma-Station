// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Silicon.BlindHealing;

public abstract partial class SharedBlindHealingSystem : EntitySystem
{
    [Serializable, NetSerializable]
    protected sealed partial class HealingDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
