// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Common.MartialArts;

[Serializable, NetSerializable]
public enum GrabStage : byte
{
    No = 0,
    Soft = 1,
    Hard = 2,
    Suffocate = 3,
}

public enum GrabStageDirection : byte
{
    Increase,
    Decrease,
}

public enum GrabResistResult : byte
{
    TooSoon,
    Failed,
    Succeeded
}
