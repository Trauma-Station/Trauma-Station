// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Virology;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DiseaseSwabComponent : Component
{
    /// <summary>
    /// EntityUid of the sampled disease.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? DiseaseUid;
}
