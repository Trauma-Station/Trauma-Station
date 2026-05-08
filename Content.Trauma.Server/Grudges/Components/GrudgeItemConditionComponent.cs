// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Grudges.Components;

[RegisterComponent]
public sealed partial class GrudgeItemConditionComponent : Component
{
    /// <summary>
    /// What is the item we're looking for?
    /// </summary>
    [DataField]
    public EntityUid? Item;

    /// <summary>
    /// Item prototype for the thing.
    /// </summary>
    [DataField]
    public EntProtoId? ItemId = "Skub";
}
