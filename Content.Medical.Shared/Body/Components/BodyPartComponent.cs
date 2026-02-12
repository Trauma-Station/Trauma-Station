// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.Surgery.Tools;
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Component for external organs aka bodyparts.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class BodyPartComponent : Component, ISurgeryToolComponent
{
    [DataField]
    public string ToolName { get; set; } = "A body part";

    [DataField]
    public float Speed { get; set; } = 1f;

    public bool? Used { get; set; }

    [DataField(required: true)]
    public BodyPartType PartType;

    [DataField, AutoNetworkedField]
    public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;

    /// <summary>
    /// Slots for organs and child bodyparts.
    /// Used by surgery, etc.
    /// </summary>
    /// <remarks>
    /// Please do not add this part's category to it I will be very sad.
    /// TODO NUBODY: Write a test that nobody ever does this
    /// </remarks>
    [DataField(required: true), AutoNetworkedField]
    public HashSet<ProtoId<OrganCategoryPrototype>> Slots = new();

    /// <summary>
    /// Child organs and bodyparts that are attached to this part.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, EntityUid> Children = new();

    /// <summary>
    /// Container to store organs in if this bodypart is severed.
    /// When being attached to a body, they will be transferred.
    /// </summary>
    // TODO SHITMED: have this affect the sprite in the future so an arm with its hand attached can be seen
    [DataField]
    public string ContainerId = "body_part_organs";
}

/// <summary>
/// Raised on a bodypart after an organ/child part was inserted into it.
/// </summary>
[ByRefEvent]
public record struct OrganInsertedIntoPartEvent(EntityUid Organ, ProtoId<OrganCategoryPrototype> Category);

/// <summary>
/// Raised on a bodypart after an organ/child part was removed from it.
/// </summary>
[ByRefEvent]
public record struct OrganRemovedFromPartEvent(EntityUid Organ, ProtoId<OrganCategoryPrototype> Category);
