// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Lavaland.Shared.Body;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class CursedHeartComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionPumpCursedHeart";

    [DataField, AutoNetworkedField]
    public EntityUid? PumpActionEntity;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan LastPump = TimeSpan.Zero;

    [DataField]
    public TimeSpan MaxDelay = TimeSpan.FromSeconds(5);
}

public sealed partial class PumpHeartActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
}
