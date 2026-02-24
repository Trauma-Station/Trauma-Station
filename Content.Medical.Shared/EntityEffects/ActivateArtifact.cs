// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Coordinates;
using Content.Shared.EntityEffects;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.EntityEffects;

/// <summary>
/// Activates the target entity, which must be an artifact.
/// </summary>
public sealed partial class ActivateArtifact : EntityEffectBase<ActivateArtifact>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed class ActivateArtifactEffectSystem : EntityEffectSystem<XenoArtifactComponent, ActivateArtifact>
{
    [Dependency] private readonly SharedXenoArtifactSystem _artifact = default!;

    protected override void Effect(Entity<XenoArtifactComponent> ent, ref EntityEffectEvent<ActivateArtifact> args)
    {
        _artifact.TryActivateXenoArtifact(ent, null, null, ent.Owner.ToCoordinates());
    }
}
