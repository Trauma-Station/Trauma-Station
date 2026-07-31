// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Shared.Body;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Common.Medical.HealthAnalyzer;

// Part selection message (from client to server)
[Serializable, NetSerializable]
public sealed class HealthAnalyzerPartMessage(ProtoId<OrganCategoryPrototype>? category) : BoundUserInterfaceMessage
{
    public readonly ProtoId<OrganCategoryPrototype>? Category = category;
}

[Serializable, NetSerializable]
public enum HealthAnalyzerMode : byte
{
    Body,
    Organs,
    Chemicals
}
