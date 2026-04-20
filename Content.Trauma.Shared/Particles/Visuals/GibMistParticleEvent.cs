// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Shared.Particles.Visuals;

/// <summary>
/// Sent from the server to nearby clients when an entity is gibbed,
/// triggering a blood-mist particle burst tinted to the entity's blood color.
/// </summary>
[Serializable, NetSerializable]
public sealed class GibMistParticleEvent : EntityEventArgs
{
    public MapCoordinates Coords;
    public Color BloodColor;

    public GibMistParticleEvent(MapCoordinates coords, Color bloodColor)
    {
        Coords = coords;
        BloodColor = bloodColor;
    }
}
