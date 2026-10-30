// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Content.Trauma.Server.Illusion;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class SpawnIllusionEffectSystem : EntityEffectSystem<MobStateComponent, SpawnIllusion>
{
    [Dependency] private EffectDataSystem _data = default!;
    [Dependency] private IllusionSystem _illusion = default!;

    protected override void Effect(Entity<MobStateComponent> ent, ref EntityEffectEvent<SpawnIllusion> args)
    {
        if (_data.GetTool(ent) is not { } tool)
            return;

        var user = args.Effect.TargetIsUser ? ent : args.User;

        _illusion.TrySpawnIllusion(tool, ent, user is not { } uid ? null : new() { uid });
    }
}
