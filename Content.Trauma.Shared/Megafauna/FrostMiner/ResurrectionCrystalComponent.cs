// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Polymorph;

namespace Content.Trauma.Shared.Megafauna.FrostMiner;

/// <summary>
/// Component added to entities that have absorbed the resurrection crystal,
/// which is dropped by the demonic frost miner.
///
/// Teleports you to a random safe location upon death, and polymorphs you.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ResurrectionCrystalComponent : Component
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;
}
