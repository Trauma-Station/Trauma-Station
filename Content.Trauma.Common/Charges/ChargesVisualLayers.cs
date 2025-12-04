// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Charges;

[Serializable, NetSerializable]
public enum ChargesVisuals : byte
{
    Charges
}

[Serializable, NetSerializable]
public enum ChargesVisualLayers : byte
{
    ChargesLeft
}
