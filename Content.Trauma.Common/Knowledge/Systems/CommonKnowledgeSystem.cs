// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Knowledge.Systems;

public abstract partial class CommonKnowledgeSystem : EntitySystem
{

    public abstract (string Category, KnowledgeInfo Info) GetKnowledgeInfo(Entity<KnowledgeComponent> knowledge);

    /// <summary>
    /// Ensures that knowledge unit exists inside an entity, and adds it if it's not already here.
    /// </summary>
    /// <returns>
    /// False if or failed to spawn a knowledge unit inside it, true if unit was found or spawned successfully.
    /// </returns>
    public abstract bool TryEnsureKnowledgeUnit(EntityUid target, EntProtoId knowledgeId, [NotNullWhen(true)] out EntityUid? found);

    /// <summary>
    /// Adds a knowledge unit to a knowledge container.
    /// </summary>
    /// <returns>
    /// False if container already has knowledge entity with that ID.
    /// </returns>
    public abstract Entity<KnowledgeComponent>? TryAddKnowledgeUnit(EntityUid target, (EntProtoId, int) knowledgeId);

    /// <summary>
    /// Adds a list of knowledge units to a knowledge container.
    /// </summary>
    public abstract void AddKnowledgeUnits(EntityUid target, Dictionary<EntProtoId, int> knowledgeList);

    /// <summary>
    /// Removes a knowledge unit from a container. Will not remove a knowledge unit if it's marked as unremoveable,
    /// unless force parameter is true.
    /// </summary>
    public abstract EntityUid? TryRemoveKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit, bool force = false);

    /// <summary>
    /// Same as TryRemoveKnowledgeUnit, but instead of removing one specific units, runs it on all knowledge units at once.
    /// </summary>
    /// <returns>
    /// False if the target is not a knowledge container.
    /// </returns>
    public abstract EntityUid? TryRemoveAllKnowledgeUnits(EntityUid target, ProtoId<KnowledgeCategoryPrototype> category, int level, bool force = false);

    /// <summary>
    /// Same as TryRemoveKnowledgeUnit, but instead of removing one specific units, runs it on all knowledge units at once.
    /// </summary>
    /// <returns>
    /// False if the target is not a knowledge container.
    /// </returns>
    public abstract EntityUid? TryRemoveAllKnowledgeUnits(EntityUid target, bool force = false);

    /// <summary>
    /// Gets a knowledge unit based on its entity prototype ID.
    /// </summary>
    /// <returns>
    /// False if the target is not a knowledge container, or if knowledge unit wasn't found.
    /// </returns>
    public abstract Entity<KnowledgeComponent>? TryGetKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit);

    /// <summary>
    /// Returns all knowledge units inside the container component.
    /// </summary>
    public abstract List<Entity<KnowledgeComponent>>? TryGetAllKnowledgeUnits(EntityUid target);

    /// <summary>
    /// Tries to get the dictionary of all knowledge units of a knowledge holder.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public abstract Dictionary<EntProtoId, EntityUid>? TryGetKnowledgeDictionary(EntityUid target);

    /// <summary>
    /// Checks if the specified component is present on any of the entity's knowledge.
    /// </summary>
    public abstract EntityUid? HasKnowledgeComp<T>(EntityUid target) where T : IComponent;

    /// <summary>
    /// Returns all knowledge that have the specified component.
    /// </summary>
    public abstract List<Entity<T, KnowledgeComponent>>? TryGetKnowledgeWithComp<T>(EntityUid target) where T : IComponent;

    /// <summary>
    /// Returns true if that knowledge can be removed, by taking
    /// into account its memory level and knowledge category.
    /// </summary>
    public abstract EntityUid? CanRemoveKnowledge(Entity<KnowledgeComponent?> target, ProtoId<KnowledgeCategoryPrototype> category, int level, bool force = false);

    /// <summary>
    /// Tries to get a knowledgeContainer.
    /// </summary>
    /// <param name="ent">Knowledge Holder</param>
    /// <returns>container</returns>
    public abstract Entity<KnowledgeContainerComponent>? TryGetKnowledgeContainer(Entity<KnowledgeHolderComponent> ent);

    /// <summary>
    /// Gets a knowledge container from an entity.
    /// Since sometimes the entity itself is a knowledge container, and sometimes it's contained in the brain,
    /// we have to sometimes relay to the brain entity to get knowledge properly.
    /// </summary>
    /// <param name="uid">Main entity from which we are trying to get</param>
    /// <returns>Entity that contains knowledge related to original uid.</returns>
    public abstract Entity<KnowledgeContainerComponent> EnsureKnowledgeContainer(Entity<KnowledgeHolderComponent> ent);

    /// <summary>
    /// Returns the KnowledgeEntity that holds the character's knowledge. Null if there is non knowledge entity found.
    /// </summary>
    public abstract EntityUid? TryGetKnowledgeEntity(EntityUid uid);

    /// <summary>
    /// Returns the KnowledgeEntity that holds the character's knowledge. Null if there is non knowledge entity found.
    /// </summary>
    public abstract EntityUid? TryGetKnowledgeEntity(Entity<KnowledgeHolderComponent> ent);

    /// <summary>
    /// Clears Knowledge from the target entity.
    /// </summary>
    public abstract void ClearKnowledge(EntityUid target, bool deleteAll);

    /// <summary>
    /// Gets the mastery level of a knowledge unit.
    /// </summary>
    /// <param name="ent"></param>
    /// <returns></returns>
    public abstract int GetMastery(Entity<KnowledgeComponent> ent);

    /// <summary>
    ///Gets the mastery level of a knowledge unit.
    /// </summary>
    public abstract int GetMastery(EntityUid uid);

    /// <summary>
    ///Gets the mastery level from a category.
    /// </summary>
    public abstract int GetInverseMastery(int number);

    /// <summary>
    /// Curve scale that determines some functionality. Goes from 0 to 1.
    /// </summary>
    public abstract float SharpCurve(Entity<KnowledgeComponent> knowledge, int offset = 0, float inverseScale = 100.0f);

    /// <summary>
    /// Inverse curve scale that determines some functionality. Goes from 1 to 0.
    /// </summary>
    public abstract float InverseSharpCurve(Entity<KnowledgeComponent> knowledge, int offset = 0, float inverseScale = 100.0f);

    /// <summary>
    /// Runs quality instructions for an item outside of the construction loop, such as the bullets for the shotgun ammo.
    /// </summary>
    /// <param name="ent"></param>
    public abstract void ModifyValues(Entity<QualityComponent> ent);
}
