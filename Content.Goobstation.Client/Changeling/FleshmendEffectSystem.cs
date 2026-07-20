// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Changeling.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Goobstation.Client.Changeling;

public sealed partial class FleshmendEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FleshmendEffectComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FleshmendEffectComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<FleshmendEffectComponent> ent, ref ComponentStartup args)
    {
        // only done if new effects were yaml'd in (or just applied to the comp)
        if (!_status.TryEffectsWithComp<FleshmendComponent>(ent, out var effects))
            return;

        foreach (var (_, effect, _) in effects)
        {
            if (effect.EffectState is { } state)
                ent.Comp.EffectState = state;
            if (effect.ResPath is { } path)
                ent.Comp.ResPath = path;

            break;
        }

        AddLayer(ent);
    }

    private void OnShutdown(Entity<FleshmendEffectComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent, sprite), FleshmendEffectKey.Key, out var layer, false))
            return;

        _sprite.RemoveLayer((ent, sprite), layer);
    }

    private void AddLayer(Entity<FleshmendEffectComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var state = ent.Comp.EffectState;

        if (_sprite.LayerMapTryGet((ent, sprite), FleshmendEffectKey.Key, out var layer, false))
        {
            _sprite.LayerSetRsiState((ent, sprite), layer, state);
            return;
        }

        var rsi = new SpriteSpecifier.Rsi(ent.Comp.ResPath, state);

        layer = _sprite.AddLayer((ent, sprite), rsi);
        _sprite.LayerMapSet((ent, sprite), FleshmendEffectKey.Key, layer);
    }
}
