// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Wraith.SaltLines
{
    [Serializable, NetSerializable]
    public enum SaltLineVisuals
    {
        ConnectedMask,
    }

    [Flags]
    [Serializable, NetSerializable]
    public enum SaltLineVisDirFlags : byte
    {
        None = 0,
        North = 1,
        South = 2,
        East = 4,
        West = 8
    }
};
