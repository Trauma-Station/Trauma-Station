// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;

namespace Content.Server._Orion.Bitrunning.Components;

[RegisterComponent]
[Access(typeof(Systems.NetpodSystem))]
public sealed partial class NetpodContainerComponent : Component
{
    public ContainerSlot BodyContainer = default!;
}
