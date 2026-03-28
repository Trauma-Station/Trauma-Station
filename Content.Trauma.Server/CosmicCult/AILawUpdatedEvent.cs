// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.CosmicCult;

/// <summary>
///     This event gets called whenever an AIs laws are actually updated.
/// </summary>
public record struct AILawUpdatedEvent(EntityUid Target, ProtoId<SiliconLawsetPrototype> Lawset);
