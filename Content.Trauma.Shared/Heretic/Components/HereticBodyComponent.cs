// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components;

/// <summary>
/// Component added to entities without a mind, previous mind of which was a heretic
/// Used for heretic-heretic sacrifices
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HereticBodyComponent : Component;
