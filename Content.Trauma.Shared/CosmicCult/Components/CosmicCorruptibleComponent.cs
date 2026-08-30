// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.CosmicCult.Components;

/// <summary>
/// Indicates that an entity will be converted to the given prototype when corrupted by the Cosmic Cult
/// </summary>
[RegisterComponent]
public sealed partial class CosmicCorruptibleComponent : Component
{
    /// <summary>
    /// What the entity turns into on corruption. Nullable for evil inheritance reasons.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId? ConvertTo;
}
