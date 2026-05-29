// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Containers;

/// <summary>
/// Empties a container on timed despawn.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DropPodSystem))]
public sealed partial class DropPodComponent : Component
{
    [DataField(required: true)]
    public string Container = string.Empty;
}
