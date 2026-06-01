// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Enchanting.Systems;
using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.Enchanting.Components;

/// <summary>
/// An item that can be sacrificed to add random enchant(s) to a target item.
/// Requires an altar with this and the target item placed on it, then click on the target with a bible.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EnchanterSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class EnchanterComponent : Component
{
    /// <summary>
    /// Minimum number of enchants to roll.
    /// </summary>
    [DataField]
    public float MinCount = 1f;

    /// <summary>
    /// Maximum number of enchants to roll.
    /// Rolled with <see cref="MinCount"/> and floored.
    /// </summary>
    [DataField]
    public float MaxCount = 1f;

    /// <summary>
    /// Minimum enchant level to roll.
    /// </summary>
    [DataField]
    public float MinLevel = 1f;

    /// <summary>
    /// Level adjustment applied to the enchantment based on the used item.
    /// </summary>
    [DataField]
    public float AdjustLevel = 0;

    /// <summary>
    /// The possible enchants that can be rolled.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId<EnchantComponent>> Enchants = new();

    /// <summary>
    /// Sound played when enchanting an item.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_Goobstation/Wizard/repulse.ogg");
}

/// <summary>
/// Sprite layer that gets hidden/shown based on <c>Enchants</c> being empty.
/// </summary>
[Serializable, NetSerializable]
public enum EnchanterVisuals : byte
{
    Layer
}
