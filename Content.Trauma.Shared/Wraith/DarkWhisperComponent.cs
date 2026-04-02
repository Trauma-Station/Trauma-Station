// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Wraith;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DarkWhisperComponent : Component
{
    /// <summary>
    /// How long this action lasts
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Update = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The entity attached to this action. Gives us the ability to speak through them
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? AttachedEntity;

    [ViewVariables, AutoNetworkedField]
    public bool Active;
}
