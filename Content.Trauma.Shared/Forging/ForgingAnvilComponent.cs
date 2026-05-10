// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Component for the anvil, which can start new unfinished items with ingots placed on it.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AnvilSystem))]
public sealed partial class ForgingAnvilComponent : Component
{
    /// <summary>
    /// Range ingots have to be in to be found.
    /// </summary>
    [DataField]
    public float IngotRange = 0.7f;

    /// <summary>
    /// Multiplier on how many ingots you need to forge something.
    /// </summary>
    [DataField]
    public int CostScale = 1;

    /// <summary>
    /// Multiplier on how much work you need to forge unfinished items.
    /// </summary>
    [DataField]
    public FixedPoint2 WorkScale = 1;

    [DataField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/_Trauma/Weapons/Melee/forging/tink.ogg");
}

[Serializable, NetSerializable]
public enum AnvilUiKey : byte
{
    Key
}

/// <summary>
/// Message sent by a player to try start a new item from ingots of a certain metal.
/// </summary>
[Serializable, NetSerializable]
public sealed class AnvilStartItemMessage(ProtoId<MetalPrototype> metal, ProtoId<ForgedItemPrototype> item) : BoundUserInterfaceMessage
{
    public readonly ProtoId<MetalPrototype> Metal = metal;
    public readonly ProtoId<ForgedItemPrototype> Item = item;
}
