// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Component for organs/bodyparts that are expected to have a parent bodypart.
/// Everything except root part (torso) should have this.
/// This component existing doesn't necessarily mean it has a parent, e.g. a severed head won't have a parent.
/// Organs inside of a severed part also won't have a parent set here.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BodyCacheSystem))]
[AutoGenerateComponentState]
public sealed partial class ChildOrganComponent : Component
{
    /// <summary>
    /// The category to look for.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> ParentCategory;

    /// <summary>
    /// The parent bodypart this organ is a child of / "inside" of.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Parent;
}
