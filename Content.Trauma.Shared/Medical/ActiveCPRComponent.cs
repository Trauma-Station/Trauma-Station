// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Medical;

/// <summary>
/// Component added to mobs which have someone performing CPR on them.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCPRSystem))]
[AutoGenerateComponentState]
public sealed partial class ActiveCPRComponent : Component
{
    /// <summary>
    /// The sound currently being played.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Sound;
}
