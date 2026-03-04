// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Trauma.Common.Knowledge.Components;

/// <summary>
/// Stores information about a set of knowledge units, assigned
/// to a dummy entity that is parented to some entity with <see cref="KnowledgeContainerComponent"/>, usually a brain.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, EntityCategory("Knowledge")]
public sealed partial class KnowledgeComponent : Component
{
    /// <summary>
    /// Category of that knowledge.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<KnowledgeCategoryPrototype> Category;

    /// <summary>
    /// Current Mastery of this knowledge.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public int Level;

    /// <summary>
    /// Current Stored experience.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Experience;

    /// <summary>
    /// Experience cost for one roll in the knowledge.
    /// </summary>
    [DataField]
    public int ExperienceCost;

    /// <summary>
    /// If true, this knowledge will become permanent, unless a system removes them forcefully.
    /// Used only for debug or admin abuse.
    /// </summary>
    [DataField]
    public bool Unremoveable;

    /// <summary>
    /// If true, the knowledge won't get displayed in the Memory tab of the character menu.
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    /// Color of the sidebar in the character UI.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Sprite to display in the character UI.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Sprite;

    /// <summary>
    /// Temporary levels that are granted by certain equipment.
    /// </summary>
    [DataField]
    public int TemporaryLevel;

    /// <summary>
    /// Temporary experience boosts that are granted by certain equipment.
    /// </summary>
    [DataField]
    public int BonusExperience;

    /// <summary>
    /// Stores the next time this component shoudl gain exp.
    /// </summary>
    [DataField]
    public TimeSpan TimeToNextExperience = TimeSpan.Zero;

    /// <summary>
    /// Determines if component uses sleep functionality.
    /// </summary>
    [DataField]
    public bool OnSleep;
}
