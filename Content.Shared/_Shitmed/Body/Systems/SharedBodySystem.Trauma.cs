using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;

namespace Content.Shared.Body.Systems;

/// <summary>
/// Trauma - extra helpers for working with body stuff
/// </summary>
public partial class SharedBodySystem
{
    /// <summary>
    /// Finds the first body part matching a given type and symmetry.
    /// </summary>
    public Entity<BodyPartComponent>? FindPart(Entity<BodyComponent?> body, BodyPartType partType, BodyPartSymmetry? symmetry = null)
    {
        foreach (var (uid, comp) in GetBodyChildrenOfType(body, partType, body.Comp, symmetry))
        {
            return (uid, comp);
        }

        return null;
    }

    /// <summary>
    /// Finds the first organ in this body part matching a given slot id.
    /// </summary>
    public Entity<OrganComponent>? FindPartOrgan(Entity<BodyPartComponent> part, string slotId)
    {
        foreach (var (uid, comp) in GetPartOrgans(part, part.Comp))
        {
            if (comp.SlotId == slotId)
                return (uid, comp);
        }

        return null;
    }

    /// <summary>
    /// Removes an organ slot from a body part.
    /// Assumes that the slot is empty beforehand.
    /// </summary>
    public bool RemoveOrganSlot(Entity<BodyPartComponent> part, string slotId)
    {
        if (!part.Comp.Organs.Remove(slotId))
            return false;

        Dirty(part);
        RemoveContainer(part, GetOrganContainerId(slotId));
        return true;
    }

    /// <summary>
    /// Removes a child part slot from a parent body part.
    /// Assumes that the slot is empty beforehand.
    /// </summary>
    public bool RemovePartSlot(Entity<BodyPartComponent> part, string slotId)
    {
        if (!part.Comp.Children.Remove(slotId))
            return false;

        Dirty(part);
        RemoveContainer(part, GetPartSlotContainerId(slotId));
        return true;
    }

    private void RemoveContainer(EntityUid uid, string id)
    {
        if (Containers.TryGetContainer(uid, id, out var container))
            Containers.ShutdownContainer(container);
    }
}
