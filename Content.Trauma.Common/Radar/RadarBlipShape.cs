// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Radar;

[Serializable, NetSerializable]
public enum RadarBlipShape
{
    Circle,
    Square,
    Triangle,
    Star,
    Diamond,
    Hexagon,
    Arrow,
    Ring
}
