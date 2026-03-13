// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Construction;

/// <summary>
/// Lets you quickly start constructions with a radial menu by using this item.
/// SS13 pressing Z on steel parity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShortConstructionComponent : Component
{
    /// <summary>
    /// Constructions this material can be turned into.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<ConstructionPrototype>> Prototypes = new();
}

[NetSerializable, Serializable]
public enum ShortConstructionUiKey : byte
{
    Key,
}
