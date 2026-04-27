// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Ui;

[Serializable, NetSerializable]
public sealed class HereticGhoulRecallUiState(List<GhoulRecallData> ghouls) : BoundUserInterfaceState
{
    public List<GhoulRecallData> Ghouls = ghouls;
}

[Serializable, NetSerializable, DataRecord]
public partial record struct GhoulRecallData(NetEntity Ent, string Name, float? Distance);

[Serializable, NetSerializable]
public sealed class HereticGhoulRecallMessage(NetEntity ghoul) : BoundUserInterfaceMessage
{
    public NetEntity Ghoul = ghoul;
}
