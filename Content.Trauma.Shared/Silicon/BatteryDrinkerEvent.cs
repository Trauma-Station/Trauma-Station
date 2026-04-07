// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Silicon;

[Serializable, NetSerializable]
public sealed partial class BatteryDrinkerDoAfterEvent : SimpleDoAfterEvent
{
    public BatteryDrinkerDoAfterEvent()
    {
    }
}
