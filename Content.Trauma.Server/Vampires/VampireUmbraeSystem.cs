// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Cloning;
using Content.Shared.Cloning;
using Content.Shared.Inventory;
using Content.Trauma.Shared.Heretic.Prototypes;
using Robust.Shared.Map;

namespace Content.Trauma.Server.Vampires;

/// <summary>
/// Public API for spawning shadow clones for the Umbrae Vampire.
/// </summary>
public sealed partial class VampireUmbraeSystem : EntitySystem
{
    [Dependency] private CloningSystem _cloning = default!;

    /// <summary>
    /// Holds components required for the shadow clone (e.g. HTN, damage values).
    /// </summary>
    private static readonly ProtoId<ComponentRegistryPrototype> ShadowCloneAddComponents = "ShadowCloneAdd";

    /// <summary>
    /// Holds components to remove (e.g. mind for ssd)
    /// </summary>
    private static readonly ProtoId<ComponentRegistryPrototype> ShadowCloneRemoveComponents = "ShadowCloneRemove";

    /// <summary>
    /// Holds the cloning settings of the shadow clones.
    /// </summary>
    private static readonly ProtoId<CloningSettingsPrototype> CloningSettings = "ShadowCloneSettings";

    /// <summary>
    /// Spawns shadow clones at a specific location.
    /// </summary>
    /// <param name="original">The user we want to clone.</param>
    /// <param name="coords">Where to spawn the clones.</param>
    /// <param name="clones">How many clones to spawn.</param>
    public void SpawnShadowClones(
        EntityUid original,
        MapCoordinates coords,
        int clones = 1)
    {
        for (int i = 0; i < clones; i++)
        {
            _cloning.TryCloning(original, coords, CloningSettings, out var clone);

            if (clone is not { } cloneEnt)
                continue;

            _cloning.CopyEquipment(original, cloneEnt, SlotFlags.All);

            EntityManager.RemoveComponents(cloneEnt, ProtoMan.Index(ShadowCloneRemoveComponents).Components);
            EntityManager.AddComponents(cloneEnt, ProtoMan.Index(ShadowCloneAddComponents).Components);
        }
    }
}
