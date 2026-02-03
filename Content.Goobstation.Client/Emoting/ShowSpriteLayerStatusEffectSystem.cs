using Content.Shared.StatusEffectNew;
using Robust.Client.GameObjects;

namespace Content.Goobstation.Client.Emoting;

public sealed class ShowSpriteLayerStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowSpriteLayerStatusEffectComponent, StatusEffectAppliedEvent>(OnApply);
        SubscribeLocalEvent<ShowSpriteLayerStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);
    }

    private void OnRemove(Entity<ShowSpriteLayerStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (TryComp(args.Target, out SpriteComponent? sprite))
            _sprite.LayerSetVisible((args.Target, sprite), ent.Comp.Layer, !ent.Comp.SetVisible);
    }

    private void OnApply(Entity<ShowSpriteLayerStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (TryComp(args.Target, out SpriteComponent? sprite))
            _sprite.LayerSetVisible((args.Target, sprite), ent.Comp.Layer, ent.Comp.SetVisible);
    }
}
