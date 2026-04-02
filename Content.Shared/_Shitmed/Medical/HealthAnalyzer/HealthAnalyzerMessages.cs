// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Medical.Common.Wounds;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.Medical.HealthAnalyzer;

// state specific to each scan mode
[Serializable, NetSerializable]
public abstract class HealthAnalyzerScanState
{
}

// Body Mode message
[Serializable, NetSerializable]
public sealed class HealthAnalyzerBodyState : HealthAnalyzerScanState
{
    public readonly Dictionary<NetEntity, List<WoundableTraumaData>> Traumas;
    public readonly Dictionary<NetEntity, FixedPoint2> NervePainFeels;

    public HealthAnalyzerBodyState(
        Dictionary<NetEntity, List<WoundableTraumaData>> traumas,
        Dictionary<NetEntity, FixedPoint2> nervePainFeels)
    {
        Traumas = traumas;
        NervePainFeels = nervePainFeels;
    }
}

// Organs Mode message
[Serializable, NetSerializable]
public sealed class HealthAnalyzerOrgansState : HealthAnalyzerScanState
{
    public readonly Dictionary<NetEntity, OrganTraumaData> Organs;

    public HealthAnalyzerOrgansState(Dictionary<NetEntity, OrganTraumaData> organs)
    {
        Organs = organs;
    }
}

// Chemicals Mode message
[Serializable, NetSerializable]
public sealed class HealthAnalyzerChemicalsState : HealthAnalyzerScanState
{
    // TODO SHITMED: use networked solution state instead of serializing?
    public readonly Dictionary<NetEntity, Solution> Solutions;

    public HealthAnalyzerChemicalsState(Dictionary<NetEntity, Solution> solutions)
    {
        Solutions = solutions;
    }
}

// Mode selection message (from client to server)
[Serializable, NetSerializable]
public sealed class HealthAnalyzerModeSelectedMessage(NetEntity? owner, HealthAnalyzerMode mode) : BoundUserInterfaceMessage
{
    public readonly NetEntity? Owner = owner;
    public readonly HealthAnalyzerMode Mode = mode;
}

// Part selection message (from client to server)
[Serializable, NetSerializable]
public sealed class HealthAnalyzerPartMessage(NetEntity? owner, ProtoId<OrganCategoryPrototype>? category) : BoundUserInterfaceMessage
{
    public readonly NetEntity? Owner = owner;
    public readonly ProtoId<OrganCategoryPrototype>? Category = category;
}

[Serializable, NetSerializable]
public struct WoundableTraumaData
{
    // TODO SHITMED: this shit should all be networked???
    public string Name;
    public string TraumaType;
    public FixedPoint2 Severity;
    public string? SeverityString; // Used mostly in Bone Damage traumas to keep track of the secondary severity.
    public (BodyPartType, BodyPartSymmetry)? TargetType;

    public WoundableTraumaData(string name,
        string traumaType,
        FixedPoint2 severity,
        string? severityString = null,
        (BodyPartType, BodyPartSymmetry)? targetType = null)
    {
        Name = name;
        TraumaType = traumaType;
        Severity = severity;
        SeverityString = severityString;
        TargetType = targetType;
    }
}

// Supporting data structures
[Serializable, NetSerializable]
public struct OrganTraumaData
{
    // TODO SHITMED: this shit should all be networked???
    public FixedPoint2 Integrity;
    public FixedPoint2 IntegrityCap;
    public OrganSeverity Severity;
    public List<(string Name, FixedPoint2 Value)> Modifiers;

    public OrganTraumaData(FixedPoint2 integrity,
        FixedPoint2 integrityCap,
        OrganSeverity severity,
        List<(string Name, FixedPoint2 Value)> modifiers)
    {
        Integrity = integrity;
        IntegrityCap = integrityCap;
        Severity = severity;
        Modifiers = modifiers;
    }
}

[Serializable, NetSerializable]
public enum HealthAnalyzerMode : byte
{
    Body,
    Organs,
    Chemicals
}
