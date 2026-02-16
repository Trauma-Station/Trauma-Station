// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Construction;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Construction.Completions;

/// <summary>
/// Applies entity effects to the construction entity.
/// </summary>
[DataDefinition]
public sealed partial class EffectGraphAction : IGraphAction
{
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    private SharedEntityEffectsSystem? _effects;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entMan)
    {
        _effects ??= entMan.System<SharedEntityEffectsSystem>();

        _effects.ApplyEffects(uid, Effects, user: userUid);
    }
}
