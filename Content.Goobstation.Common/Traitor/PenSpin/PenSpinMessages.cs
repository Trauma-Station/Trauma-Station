// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Goobstation.Common.Traitor.PenSpin;

[Serializable, NetSerializable]
public sealed class PenSpinSubmitDegreeMessage : BoundUserInterfaceMessage
{
    public int Degree { get; }

    public PenSpinSubmitDegreeMessage(int degree)
    {
        Degree = degree;
    }
}

[Serializable, NetSerializable]
public sealed class PenSpinResetMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum PenSpinUiKey : byte
{
    Key
}
