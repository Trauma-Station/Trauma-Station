// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Disease;

/// <summary>
/// Component for a disease effect to make it check <see cref="DnaTargetDiseaseComponent.Enabled"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseDnaTargetConditionComponent : Component;
