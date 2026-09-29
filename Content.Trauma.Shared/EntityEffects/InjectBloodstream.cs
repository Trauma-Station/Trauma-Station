// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

namespace Content.Trauma.Shared.EntityEffects.Effects;

/// <summary>
/// Injects ent with set amount of solution.
/// </summary>
public sealed partial class InjectBloodstream : EntityEffectBase<InjectBloodstream>
{
    [DataField(required: true)]
    public Solution Solution;
}

public sealed partial class InjectSystem : EntityEffectSystem<SolutionManagerComponent, InjectBloodstream>
{
    [Dependency] private BloodstreamSystem _bloodstremSys = default!;

    protected override void Effect(Entity<SolutionManagerComponent> ent, ref EntityEffectEvent<InjectBloodstream> args)
    {
        var flag = _bloodstremSys.TryAddToBloodstream(ent.Owner, args.Effect.Solution);
    }
}