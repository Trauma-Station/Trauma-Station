// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Lock;

public sealed class AccessDeniedSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, BeforeAccessReaderCheckEvent>(OnBeforeCheck);
    }

    private void OnBeforeCheck(Entity<StatusEffectContainerComponent> ent, ref BeforeAccessReaderCheckEvent args)
    {
        if (_status.HasEffectComp<AccessDeniedStatusEffectComponent>(ent))
            args.Cancelled = true;
    }
}
