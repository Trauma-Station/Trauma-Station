// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Common.Research;

[Serializable, NetSerializable]
public enum ResearchAvailability : byte
{
    Researched,
    Available,
    PrereqsMet,
    Unavailable
}
