// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Disease;

/// <summary>
/// Disease effect that removes the disease from the carrier.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseRemoveEffectComponent : Component;
