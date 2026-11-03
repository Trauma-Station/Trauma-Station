// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Audio;

namespace Content.Trauma.Shared.BloodCult.Runes.BloodBoil;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultRuneBloodBoilComponent : Component
{
    [DataField]
    public EntProtoId ProjectilePrototype = "BloodBoilProjectile";

    [DataField]
    public float ProjectileSpeed = 50;

    [DataField]
    public float TargetsLookupRange = 15f;

    [DataField]
    public int ProjectileCount = 3;

    /// <summary>
    /// Effects to apply to a random target for every projectile spawned.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects;

    [DataField]
    public SoundSpecifier ActivationSound = new SoundPathSpecifier("/Audio/_Trauma/BloodCult/magic.ogg");
}
