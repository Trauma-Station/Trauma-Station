// <Trauma>
using Content.Client.Light.Visualizers;
using Content.Client.PowerCell;
using Content.Client.Weapons.Ranged.Components;
using Content.Trauma.Common.Sprite;
// </Trauma>
using Content.Client.Effects;
using Content.Client.Smoking;
using Content.Shared.Chemistry.Components;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Polymorph.Systems;

public sealed partial class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    // <Trauma>
    [Dependency] private CommonSpriteVisibilitySystem _spriteVis = default!;
    // </Trauma>
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery = default!;
    // [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!; // Trauma

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonDisguiseComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<ChameleonDisguisedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChameleonDisguisedComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChameleonDisguisedComponent, GetFlashEffectTargetEvent>(OnGetFlashEffectTargetEvent);
    }

    private void OnHandleState(Entity<ChameleonDisguiseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        CopySprite(ent);
        CopyComp<GenericVisualizerComponent>(ent);
        CopyComp<SolutionContainerVisualsComponent>(ent);
        CopyComp<BurnStateVisualsComponent>(ent);
        // <Trauma>
        CopyComp<PowerChargerVisualsComponent>(ent);
        CopyComp<MagazineVisualsComponent>(ent);
        CopyComp<PoweredLightVisualsComponent>(ent);
        // </Trauma>

        // reload appearance to hopefully prevent any invisible layers
        if (_appearanceQuery.TryComp(ent, out var appearance))
            _appearance.QueueUpdate(ent, appearance);
    }

    /// <summary>
    /// Copies the source entity/prototype's sprite onto the disguise.
    /// </summary>
    private void CopySprite(Entity<ChameleonDisguiseComponent> ent)
    {
        if (!GetSrcEntity<SpriteComponent>(ent.Comp, out var src))
            return;

        var dest = EnsureComp<SpriteComponent>(ent);
        _sprite.CopySprite(src, (ent.Owner, dest));
    }

    private void OnStartup(Entity<ChameleonDisguisedComponent> ent, ref ComponentStartup args)
    {
        // <Trauma>
        _spriteVis.UpdateVisibilityModifiers(ent, nameof(ChameleonDisguisedComponent), 0f);
        /*
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        ent.Comp.WasVisible = sprite.Visible;
        _sprite.SetVisible((ent.Owner, sprite), false);
        </Trauma> */
    }

    private void OnShutdown(Entity<ChameleonDisguisedComponent> ent, ref ComponentShutdown args)
    {
        // <Trauma>
        _spriteVis.UpdateVisibilityModifiers(ent, nameof(ChameleonDisguisedComponent), 1f);
        /*
        if (_spriteQuery.TryComp(ent, out var sprite))
            _sprite.SetVisible((ent.Owner, sprite), ent.Comp.WasVisible);
        </Trauma> */
    }

    private void OnGetFlashEffectTargetEvent(Entity<ChameleonDisguisedComponent> ent, ref GetFlashEffectTargetEvent args)
    {
        args.Target = ent.Comp.Disguise;
    }
}
