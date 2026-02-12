// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Body.Part;

/// <summary>
/// Gives this entity an action to burrow into the target mob's body part of a given type.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EnterBodyPartSystem))]
[AutoGenerateComponentState]
public sealed partial class EnterBodyPartComponent : Component
{
    /// <summary>
    /// The type of body part to burrow into.
    /// Please do not use a internal organ :)
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    /// <summary>
    /// The action to add.
    /// </summary>
    [DataField]
    public EntProtoId<EntityTargetActionComponent> Action = "ActionEnterBodyPart";

    /// <summary>
    /// The added action entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// How long the burrowing doafter lasts.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(6);
}

public sealed partial class EnterBodyPartActionEvent : EntityTargetActionEvent;
