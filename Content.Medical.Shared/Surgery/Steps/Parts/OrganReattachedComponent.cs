// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Surgery.Steps.Parts;

/// <summary>
/// Added to reattached bodyparts or internal organs just before the surgery is finished.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganReattachedComponent : Component;
