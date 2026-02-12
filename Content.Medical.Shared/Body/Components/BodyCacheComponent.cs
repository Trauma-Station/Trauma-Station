// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Body component that caches which organs are in each slot for faster lookups.
/// Also responsible for <see cref="ChildOrganComponent"/> management.
/// </summary>
// TODO NUBODY: write a test that this never desyncs with reality
[RegisterComponent, NetworkedComponent, Access(typeof(BodyCacheSystem))]
[AutoGenerateComponentState]
public sealed partial class BodyCacheComponent : Component
{
    /// <summary>
    /// The cached organ dictionary.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> Organs = new();
}
