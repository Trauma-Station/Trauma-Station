// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.GrantTemporatyComponent;

/// <summary>
/// Forgive me Deltanidas for i have sined.
/// Stores temporary components and their lifespan. Assumes that all temporary comp's will need only one lifespan.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrantTemporatyComponentComponent : Component
{
    [DataField]
    public ComponentRegistry Components;

    [DataField, AutoNetworkedField]
    public TimeSpan DeleteAfter = default!;
}
