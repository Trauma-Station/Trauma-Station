// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Interaction;

/// <summary>
/// Lets this entity skip the cross-container interaction checks in <see cref="ContainerInteractionSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CrossContainerInteractionComponent : Component;
