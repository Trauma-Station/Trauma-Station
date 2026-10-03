// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Spawners;

/// <summary>
/// Timed despawn that updates inside prediction properly for deletion of clientside entities.
/// Does nothing for networked entities.
/// </summary>
[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class TimedClientDespawnComponent : Component
{
    [DataField(required: true)]
    public TimeSpan Lifetime;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextDespawn;
}
