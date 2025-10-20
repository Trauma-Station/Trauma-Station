using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Paint;

/// <summary>
/// Component for spray paint cans that add <see cref="PaintVisualsComponent"/> to a valid target entity on interact.
/// Requires <c>EffectsToolComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(PaintSystem))]
public sealed partial class PaintCanComponent : Component
{
    /// <summary>
    /// The color of paint used.
    /// </summary>
    [DataField(required: true)]
    public Color Color;
}

/// <summary>
/// Event raised on an entity to check if it can be painted.
/// Cancelling handlers are expected to show a popup.
/// </summary>
[ByRefEvent]
public record struct PaintAttemptEvent(Entity<PaintCanComponent> Can, Color Color, EntityUid User, bool Cancelled = false);

[Serializable, NetSerializable]
public enum PaintCanVisuals : byte
{
    Layer // gets changed to the can's color if present
}
