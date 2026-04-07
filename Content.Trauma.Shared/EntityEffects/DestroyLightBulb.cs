// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Destroys the target light's bulb/tube.
/// </summary>
public sealed partial class DestroyLightBulb : EntityEffectBase<DestroyLightBulb>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}

public sealed class DestroyLightBulbEffectSystem : EntityEffectSystem<PoweredLightComponent, DestroyLightBulb>
{
    [Dependency] private readonly SharedPoweredLightSystem _light = default!;

    protected override void Effect(Entity<PoweredLightComponent> ent, ref EntityEffectEvent<DestroyLightBulb> args)
    {
        _light.TryDestroyBulb(ent, ent.Comp, user: args.User);
    }
}
