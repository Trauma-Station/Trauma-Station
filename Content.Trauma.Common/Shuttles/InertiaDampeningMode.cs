// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Shuttles;

[Serializable, NetSerializable]
public enum InertiaDampeningMode : byte
{
    None = 0, // Reserved for requests - does not set the mode, only returns its state.
    Cruise,
    Dampen,
    Anchor,
}
