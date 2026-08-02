// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;

namespace Content.Medical.Shared.Traumas;

/// <summary>
/// Component used for dismemberment leftover wounds that makes them distinct if the source
/// part (e.g. arms legs) is different, allowing multiple wounds on the dest part (torso).
/// </summary>
[RegisterComponent, NetworkedComponent]
[EntityCategory("Traumas")]
[AutoGenerateComponentState]
public sealed partial class AmputationTraumaComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<OrganCategoryPrototype> Source;
}
