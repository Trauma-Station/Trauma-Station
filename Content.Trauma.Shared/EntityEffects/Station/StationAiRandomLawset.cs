// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects.Station;

/// <summary>
/// Station effect that gives the station's AI a random lawset from a list.
/// </summary>
public sealed partial class StationAiRandomLawset : EntityEffectBase<StationAiRandomLawset>
{
    [DataField(required: true)]
    public List<ProtoId<SiliconLawsetPrototype>> Lawsets = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}
