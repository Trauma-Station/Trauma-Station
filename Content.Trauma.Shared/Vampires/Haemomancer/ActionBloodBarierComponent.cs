// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;

namespace Content.Trauma.Shared.Vampires.Haemomancer;

/// <summary>
/// Action component that spawns an entity prototype between 2 points.
/// Using the action on a point you selected will clear it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ActionBloodBarrierComponent : Component
{
    /// <summary>
    /// The entity prototype to spawn between point A and point B.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId BarrierProto;

    /// <summary>
    /// The distance required from point A and point B.
    /// </summary>
    [DataField]
    public float Distance = 3f;

    /// <summary>
    /// The entities of the points we currently have stored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Points = new();

    /// <summary>
    /// Visual representation of the points.
    /// Gets deleted when <see cref="Points"/> are cleared.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId PointProto;
}

public sealed partial class BloodBarrierActionEvent : WorldTargetActionEvent;
