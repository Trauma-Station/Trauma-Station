using System.Diagnostics.CodeAnalysis;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.Knowledge;
using Robust.Shared.Containers;
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
    public abstract bool TryAddKnowledgeUnit(EntityUid target, EntProtoId knowledgeId);

    /// <inheritdoc cref="TryAddKnowledgeUnit(Robust.Shared.GameObjects.EntityUid,Robust.Shared.Prototypes.EntProtoId)"/>
    public abstract bool TryAddKnowledgeUnit(EntityUid target, EntProtoId knowledgeId, [NotNullWhen(true)] out EntityUid? found);

    /// <summary>
    /// Adds a list of knowledge units to a knowledge container.
    /// </summary>
    public abstract void AddKnowledgeUnits(EntityUid target, List<EntProtoId> knowledgeList);

    /// <summary>
    /// Removes a knowledge unit from a container. This version takes into account levels and categories of knowledge.
    /// If knowledge has higher level than specified in the method, or a different category, it will not be removed.
    /// </summary>
    /// <param name="target">Entity to remove a unit from.</param>
    /// <param name="knowledgeUnit">Knowledge unit to remove.</param>
    /// <param name="category">Category of knowledge that we are removing.</param>
    /// <param name="level">Level of removal, that will remove knowledge only if its level is lower or equal to that value.</param>
    /// <param name="force">If true, will override all checks and will just always remove this knowledge.</param>
    /// <returns>True if removed successfully.</returns>
    public abstract EntityUid? TryRemoveKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit, ProtoId<KnowledgeCategoryPrototype> category, int level, bool force = false);

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
    public abstract EntityUid? TryGetKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit);

    /// <summary>
    /// Checks if that knowledge unit already exists inside a knowledge container.
    /// </summary>
    /// <returns>
    /// False if the target is not a knowledge container, and true if knowledge unit with that ID was found.
    /// </returns>
    public abstract EntityUid? HasKnowledgeUnit(EntityUid target, EntProtoId knowledgeUnit);

    /// <summary>
    /// Returns all knowledge units inside the container component.
    /// </summary>
    public abstract List<Entity<KnowledgeComponent>>? TryGetAllKnowledgeUnits(EntityUid target);

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
    /// Gets a knowledge container from an entity.
    /// Since sometimes the entity itself is a knowledge container, and sometimes it's contained in the brain,
    /// we have to sometimes relay to the brain entity to get knowledge properly.
    /// </summary>
    /// <param name="uid">Main entity from which we are trying to get</param>
    /// <returns>Entity that contains knowledge related to original uid.</returns>
    public abstract Entity<KnowledgeContainerComponent> EnsureKnowledgeContainer(EntityUid uid);
    /// <inheritdoc cref="EnsureKnowledgeContainer(Robust.Shared.GameObjects.EntityUid)"/>
    public abstract void EnsureKnowledgeContainer(EntityUid uid, out Entity<KnowledgeContainerComponent> container);
}
