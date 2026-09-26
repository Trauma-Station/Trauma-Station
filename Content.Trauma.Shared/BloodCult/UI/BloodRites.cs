// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.BloodCult.UI;

[NetSerializable, Serializable]
public enum BloodRitesUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class BloodRitesMessage(EntProtoId selectedProto) : BoundUserInterfaceMessage
{
    public EntProtoId SelectedProto = selectedProto;
}
