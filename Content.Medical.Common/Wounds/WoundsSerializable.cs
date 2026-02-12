// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Serialization;

namespace Content.Medical.Common.Wounds;

[Serializable, NetSerializable]
public enum WoundType : byte
{
    External,
    Internal,
}

[Serializable, NetSerializable]
public enum WoundSeverity : byte
{
    Healed,
    Minor,
    Moderate,
    Severe,
    Critical,
    Loss,
}

[Serializable, NetSerializable]
public enum BleedingSeverity : byte
{
    Minor,
    Severe,
}

[Serializable, NetSerializable]
public enum WoundableSeverity : byte
{
    Healthy,
    Minor,
    Moderate,
    Severe,
    Critical,
    Mangled,
    Severed,
}

[Serializable, NetSerializable]
public enum WoundVisibility : byte
{
    Always,
    HandScanner,
    AdvancedScanner,
}

[Serializable, NetSerializable]
public enum WoundableVisualizerKeys : byte
{
    Wounds,
}

[Serializable, NetSerializable]
public sealed class WoundVisualizerGroupData : ICloneable
{
    public List<NetEntity> GroupList;

    public WoundVisualizerGroupData(List<NetEntity> groupList)
    {
        GroupList = groupList;
    }

    public object Clone()
    {
        return new WoundVisualizerGroupData(new List<NetEntity>(GroupList));
    }
}
