// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Durability.Events;

[Serializable, NetSerializable]
public sealed partial class RepairItemDoAfterEvent : DoAfterEvent
{
    [DataField]
    public Vector2 MinMax;

    private RepairItemDoAfterEvent()
    {

    }

    public RepairItemDoAfterEvent(Vector2 minMax)
    {
        MinMax = minMax;
    }

    public override DoAfterEvent Clone()
    {
        return new RepairItemDoAfterEvent(MinMax);
    }
};

[Serializable, NetSerializable]
public sealed partial class RepairToolDoAfterEvent : SimpleDoAfterEvent;
