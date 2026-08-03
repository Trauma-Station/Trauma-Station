// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.AlertLevel;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.AlertLevel;

/// <summary>
/// Station component that locks an alert level, only allowing it once a required level has been active for a certain time.
/// This is ignored by the admin set alert level command or any other forced set level mechanism.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AlertLevelLockingSystem))]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AlertLevelLockingComponent : Component
{
    /// <summary>
    /// The alert level that must be active for <see cref="LockTime"> to unlock <see cref="LockedLevel"/>.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertLevelPrototype> RequiredLevel;

    /// <summary>
    /// The alert level to be locked.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertLevelPrototype> LockedLevel;

    /// <summary>
    /// How long <see cref="RequiredLevel"/> must be active for <see cref="LockedLevel"/> to unlock.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan LockTime = TimeSpan.Zero;

    /// <summary>
    /// When <see cref="LockedLevel"/> can be used.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextUnlock;
}
