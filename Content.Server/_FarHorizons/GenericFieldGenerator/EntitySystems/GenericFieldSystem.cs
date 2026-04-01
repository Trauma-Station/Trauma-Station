using Content.Shared._FarHorizons.GenericFieldGenerator.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._FarHorizons.GenericFieldGenerator.EntitySystems;

public sealed class GenericFieldSystem : EntitySystem
{
    [Dependency] private readonly GenericFieldGeneratorSystem _genericgen = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericFieldComponent, DestructionEventArgs>(OnDestructionEvent);
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
        _genericgen.FieldDestroyed(field.Comp.SourceGen.Value);
    }

    public void TempTileCleanup(Entity<GenericFieldComponent> field)
    {
        var fieldXForm = Transform(field);

        if (field.Comp.TempTile && !TerminatingOrDeleted(fieldXForm.ParentUid))
        {
            var gridUid = fieldXForm.ParentUid;

            if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
                return;

            var tileref = _mapSystem.GetTileRef(gridUid, mapGrid, _transformSystem.GetMapCoordinates(field, fieldXForm));

            if (tileref.Tile.IsEmpty)
            {
                field.Comp.TempTile = false;
                return;
            }

            _mapSystem.SetTile((gridUid, mapGrid), fieldXForm.Coordinates, Tile.Empty);
            field.Comp.TempTile = false;
        }
    }
}