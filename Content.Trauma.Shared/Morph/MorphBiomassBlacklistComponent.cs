// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Morph;

/// <summary>
/// Prevents a morph from getting biomass when eating this entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MorphBiomassBlacklistComponent : Component;
