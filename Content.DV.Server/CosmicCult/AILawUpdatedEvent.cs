using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

namespace Content.DV.Server.CosmicCult;

/// <summary>
///     This event gets called whenever an AIs laws are actually updated.
/// </summary>
public record struct AILawUpdatedEvent(EntityUid Target, ProtoId<SiliconLawsetPrototype> Lawset);
