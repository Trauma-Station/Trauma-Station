using Content.Shared._FarHorizons.GenericFieldGenerator.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server._FarHorizons.GenericFieldGenerator.EntitySystems;

public sealed class GenericFieldSystem : EntitySystem
{
    [Dependency] private readonly GenericFieldGeneratorSystem _genericgen = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericFieldComponent, DestructionEventArgs>(OnDestructionEvent);
        SubscribeLocalEvent<GenericFieldComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<GenericFieldComponent, ComponentRemove>(OnComponentRemoved);
    }
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GenericFieldComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var field, out var damageable))
        {
            field.Accumulator += frameTime;
            if (!(field.Accumulator >= field.Threshold)) continue;
            
            field.Accumulator -= field.Threshold; 
            _damageable.HealEvenly((uid, damageable), field.RegenRate);
        }
    }

    private void OnDestructionEvent(Entity<GenericFieldComponent> field, ref DestructionEventArgs args)
    {
        if (field.Comp.SourceGen == null)
            return;
        TempTileCleanup(field);
        _genericgen.FieldDestroyed(field.Comp.SourceGen.Value);
    }

    public void TempTileCleanup(Entity<GenericFieldComponent> field)
    {
        if (field.Comp.TempTile && !TerminatingOrDeleted(field.Comp.GridUid))
        {
            if (!_tiledef.TryGetDefinition("Space", out var tileDef))
                return;

            _tile.ReplaceTile(field.Comp.Tileref, (ContentTileDefinition)tileDef, field.Comp.GridUid, field.Comp.MapGrid);
            field.Comp.TempTile = false;
        }
    }

    private void OnAnchorChanged(Entity<GenericFieldComponent> field, ref AnchorStateChangedEvent args) // tile beneath removed, likely destroyed
    {
        if (!args.Anchored && field.Comp.SourceGen != null)
            _genericgen.FieldDestroyed(field.Comp.SourceGen.Value);
    }

    private void OnComponentRemoved(Entity<GenericFieldComponent> field, ref ComponentRemove args) => TempTileCleanup(field); // failsafe if field is somehow deleted by something unexpectedly

}
//Space