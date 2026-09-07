// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Medical.Common.Wounds;

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
