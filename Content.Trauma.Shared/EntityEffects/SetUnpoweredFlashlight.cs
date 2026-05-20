// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that sets the light on an entity with <see cref="UnpoweredFlashlightComponent"/>.
/// </summary>
public sealed partial class SetUnpoweredFlashlight : EntityEffectBase<SetUnpoweredFlashlight>
{
    /// <summary>
    /// Either disables or enables the flashlight.
    /// </summary>
    [DataField]
    public bool Activated;

    /// <summary>
    /// Will it make a sound, or not?
    /// </summary>
    [DataField]
    public bool Quiet;
}

public sealed partial class SetUnpoweredFlashlightEffectSystem : EntityEffectSystem<UnpoweredFlashlightComponent, SetUnpoweredFlashlight>
{
    [Dependency] private UnpoweredFlashlightSystem _unpowered = default!;

    protected override void Effect(Entity<UnpoweredFlashlightComponent> entity, ref EntityEffectEvent<SetUnpoweredFlashlight> args)
    {
        var effect = args.Effect;
        _unpowered.SetLight(entity.Owner, effect.Activated, entity.Owner, effect.Quiet);
    }
}
