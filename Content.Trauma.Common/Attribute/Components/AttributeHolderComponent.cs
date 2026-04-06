// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Attribute.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AttributeHolderComponent : Component
{
    /// <summary>
    /// Pointer to the actual entity with AttributeContainerComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AttributeEntity;

    /// <summary>
    /// Stores the synchronization between the body and mind per attribute.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, FixedPoint2> Synchronization = new();
}
