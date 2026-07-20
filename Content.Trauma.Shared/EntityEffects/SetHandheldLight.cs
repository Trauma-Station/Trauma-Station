// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Light;
using Content.Shared.Light.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that sets the light on an entity with <see cref="HandheldLightComponent"/>.
/// </summary>
public sealed partial class SetHandheldLight : EntityEffectBase<SetHandheldLight>
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

public sealed partial class DisableFlashlightEffectSystem : EntityEffectSystem<HandheldLightComponent, SetHandheldLight>
{
    [Dependency] private SharedHandheldLightSystem _handheld = default!;

    protected override void Effect(Entity<HandheldLightComponent> entity, ref EntityEffectEvent<SetHandheldLight> args)
    {
        var effect = args.Effect;
        _handheld.SetActivated(entity.Owner, effect.Activated, entity.Comp, effect.Quiet);
    }
}
