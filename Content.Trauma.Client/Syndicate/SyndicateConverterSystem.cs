using Content.Trauma.Shared.Syndicate;
using Content.Trauma.Shared.Syndicate.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Syndicate;

/// <inheritdoc/>
public sealed partial class SyndicateConverterSystem : SharedSyndicateConverterSystem
{
    //[Dependency] private AppearanceSystem _appearance = default!;
    //[Dependency] private SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<FlatpackComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    /*
    private void OnAppearanceChange(Entity<FlatpackComponent> ent, ref AppearanceChangeEvent args)
    {
        var (_, comp) = ent;
        if (!_appearance.TryGetData<string>(ent, FlatpackVisuals.Machine, out var machineBoardId) || args.Sprite == null)
            return;

        if (!ProtoMan.TryIndex<EntityPrototype>(machineBoardId, out var machineBoardPrototype))
            return;

        if (!machineBoardPrototype.TryComp(out SpriteComponent? sprite, EntityManager.ComponentFactory))
            return;

        Color? color = null;
        foreach (var layer in sprite.AllLayers)
        {
            if (layer.RsiState.Name is not { } spriteState)
                continue;

            if (!comp.BoardColors.TryGetValue(spriteState, out var c))
                continue;
            color = c;
            break;
        }

        if (color != null)
            _sprite.LayerSetColor((ent.Owner, args.Sprite), FlatpackVisualLayers.Overlay, color.Value);
    } */
}
