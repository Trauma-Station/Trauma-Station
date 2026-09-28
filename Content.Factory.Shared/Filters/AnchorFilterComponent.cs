// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Factory.Shared.Filters;

/// <summary>
/// Filter that requires an anchorable entity, and allows it if <c>Anchored == ItemToggleComponent.Activated</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AnchorFilterComponent : Component;
