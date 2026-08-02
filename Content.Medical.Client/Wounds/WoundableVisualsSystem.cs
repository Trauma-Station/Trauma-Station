// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Medical.Client.Wounds;

/// <summary>
/// Handles visual representation of wounds and damage on body parts
/// </summary>
public sealed partial class WoundableVisualsSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private EntityQuery<BleedInflicterComponent> _bleedQuery = default!;
    [Dependency] private EntityQuery<VisualBodyComponent> _visualBodyQuery = default!;
    [Dependency] private EntityQuery<VisualOrganComponent> _visualQuery = default!;
    [Dependency] private EntityQuery<WoundableVisualsComponent> _query = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    private const float AltBleedingSpriteChance = 0.15f;
    private const string BleedingSuffix = "Bleeding";
    private const string MinorSuffix = "Minor";

    private Enum? GetLayer(EntityUid uid)
        => _visualQuery.CompOrNull(uid)?.Layer;

    private void InitBleeding(Entity<WoundableVisualsComponent> ent)
    {
        if (_body.GetBody(ent.Owner) is not {} body ||
            ent.Comp.BleedingOverlay is not {} overlay ||
            !_visualBodyQuery.HasComp(body) ||
            !_spriteQuery.TryComp(body, out var sprite) ||
            GetLayer(ent) is not {} layer)
            return;

        AddDamageLayerToSprite((body, sprite), overlay, BuildStateKey(layer, MinorSuffix), BuildLayerKey(layer, BleedingSuffix));
    }

    private void InitDamage(Entity<WoundableVisualsComponent> ent)
    {
        if (_body.GetBody(ent.Owner) is not {} body ||
            !_visualBodyQuery.HasComp(body) ||
            !_spriteQuery.TryComp(body, out var spriteComp) ||
            GetLayer(ent) is not {} layer)
            return;

        foreach (var (group, sprite) in ent.Comp.DamageGroupSprites)
        {
            var color = GetColor(ent, group);
            AddDamageLayerToSprite((body, spriteComp),
                sprite,
                BuildStateKey(layer, group, "100"),
                BuildLayerKey(layer, group),
                color);
        }
    }

    #region Event Handlers

    [SubscribeLocalEvent]
    private void OnWoundableInserted(Entity<WoundableVisualsComponent> ent, ref OrganGotInsertedEvent args)
    {
        InitDamage(ent);
        InitBleeding(ent);

        var body = args.Target;
        if (!_visualBodyQuery.HasComp(body) ||
            !_spriteQuery.TryComp(body, out var sprite) ||
            GetLayer(ent) is not {} layer)
            return;

        if (ent.Comp.DamageGroupSprites != null)
        {
            foreach (var (group, rsiPath) in ent.Comp.DamageGroupSprites)
            {
                if (_sprite.LayerMapTryGet((body, sprite), BuildLayerKey(layer, group), out _, false))
                    continue;

                var color = GetColor(ent, group);
                AddDamageLayerToSprite((body, sprite),
                    rsiPath,
                    BuildStateKey(layer, group, "100"),
                    BuildLayerKey(layer, group),
                    color);
            }
        }

        if (!_sprite.LayerMapTryGet((body, sprite), BuildLayerKey(layer, BleedingSuffix), out _, false)
            && ent.Comp.BleedingOverlay is {} overlay)
        {
            AddDamageLayerToSprite((body, sprite),
                overlay,
                BuildStateKey(layer, MinorSuffix),
                BuildLayerKey(layer, BleedingSuffix));
        }

        UpdateWoundableVisuals(ent, (body, sprite));
    }

    [SubscribeLocalEvent]
    private void OnWoundableRemoved(Entity<WoundableVisualsComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveWoundableLayers(args.Target.Owner, ent);
    }

    [SubscribeLocalEvent]
    private void OnWoundableHandleState(Entity<WoundableComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdatePartVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnWoundHandleState(Entity<WoundComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdatePartVisuals(ent.Comp.HoldingWoundable);
    }
    #endregion

    private void UpdatePartVisuals(EntityUid uid)
    {
        if (!_query.TryComp(uid, out var visuals))
            return;

        if (_body.GetBody(uid) is {} body)
            UpdateWoundableVisuals((uid, visuals), body);
        else
            UpdateWoundableVisuals((uid, visuals), uid); // use part's sprite
    }

    #region Layer Management
    private void RemoveWoundableLayers(Entity<SpriteComponent?> ent, Entity<WoundableVisualsComponent> visuals)
    {
        if (!_spriteQuery.Resolve(ent, ref ent.Comp) || GetLayer(visuals) is not {} partLayer)
            return;

        foreach (var (group, _) in visuals.Comp.DamageGroupSprites)
        {
            var layerKey = BuildLayerKey(partLayer, group);
            if (!_sprite.LayerMapTryGet(ent, layerKey, out var layer, false))
                continue;

            _sprite.LayerSetVisible(ent, layer, false);
            _sprite.RemoveLayer(ent, layer);
            _sprite.LayerMapRemove(ent, layerKey);
        }

        var bleedingKey = BuildLayerKey(partLayer, BleedingSuffix);
        if (!_sprite.LayerMapTryGet(ent, bleedingKey, out var bleedLayer, false))
            return;

        _sprite.LayerSetVisible(ent, bleedLayer, false);
        _sprite.RemoveLayer(ent, bleedLayer, out _, false);
        _sprite.LayerMapRemove(ent, bleedingKey, out _);
    }

    private void AddDamageLayerToSprite(Entity<SpriteComponent?> ent,
        string sprite,
        string state,
        string mapKey,
        Color? color = null)
    {
        if (!_spriteQuery.Resolve(ent, ref ent.Comp) || _sprite.LayerExists(ent, mapKey)) // prevent dupes
            return;

        var newLayer = _sprite.AddLayer(ent,
            new SpriteSpecifier.Rsi(
                new ResPath(sprite),
                state
            ));
        _sprite.LayerMapSet(ent, mapKey, newLayer);
        if (color != null)
            _sprite.LayerSetColor(ent, newLayer, color.Value);
        _sprite.LayerSetVisible(ent, newLayer, false);
    }
    #endregion

    #region Visual Updates
    private void UpdateWoundableVisuals(Entity<WoundableVisualsComponent> visuals, Entity<SpriteComponent?> sprite)
    {
        if (!_spriteQuery.Resolve(sprite, ref sprite.Comp))
            return;

        UpdateDamageVisuals(visuals, sprite);
        UpdateBleedingVisuals(visuals, sprite);
    }

    private void UpdateDamageVisuals(Entity<WoundableVisualsComponent> visuals, Entity<SpriteComponent?> sprite)
    {
        if (GetLayer(visuals) is not {} layer)
            return;

        foreach (var group in visuals.Comp.DamageGroupSprites)
        {
            if (!_sprite.LayerMapTryGet(sprite, $"{layer}{group.Key}", out var damageLayer, false))
                continue;

            var severityPoint = _wound.GetWoundableSeverityPoint(visuals.Owner, damageGroup: group.Key);
            UpdateDamageLayerState(sprite,
                damageLayer,
                BuildStateKey(layer, group.Key),
                GetThreshold(severityPoint, visuals));
        }
    }
    private void UpdateBleedingVisuals(Entity<WoundableVisualsComponent> ent, Entity<SpriteComponent?> sprite)
    {
        if (ent.Comp.BleedingOverlay is null)
            UpdateParentBleedingVisuals(ent, sprite);
        else
            UpdateOwnBleedingVisuals(ent, sprite);
    }

    private void UpdateParentBleedingVisuals(
        Entity<WoundableVisualsComponent> woundable,
        Entity<SpriteComponent?> sprite)
    {
        if (TerminatingOrDeleted(woundable) ||
            !TryComp<BodyPartComponent>(woundable, out var part) ||
            _part.GetParentPart(woundable.Owner) is not {} parent ||
            TerminatingOrDeleted(parent))
            return;

        var partKey = GetLimbBleedingKey(part);
        var layerKey = BuildLayerKey(partKey, BleedingSuffix);
        var totalBleeds = FixedPoint2.Zero;
        totalBleeds += CalculateTotalBleeding(woundable);
        totalBleeds += CalculateTotalBleeding(parent);

        if (!_sprite.LayerMapTryGet(sprite, layerKey, out var layer, false))
            return;

        var threshold = CalculateBleedingThreshold(totalBleeds, woundable.Comp);
        UpdateBleedingLayerState(sprite, layer, partKey, totalBleeds, threshold);
    }

    private void UpdateOwnBleedingVisuals(Entity<WoundableVisualsComponent> woundable, Entity<SpriteComponent?> sprite)
    {
        if (GetLayer(woundable) is not {} partLayer)
            return;

        var layerKey = BuildLayerKey(partLayer, BleedingSuffix);
        if (!_sprite.LayerMapTryGet(sprite, layerKey, out var layer, false))
            return;

        var totalBleeds = CalculateTotalBleeding(woundable);
        var threshold = CalculateBleedingThreshold(totalBleeds, woundable.Comp);
        UpdateBleedingLayerState(sprite, layer, partLayer.ToString(), totalBleeds, threshold);
    }

    #endregion
    #region Helper Methods
    private Color? GetColor(WoundableVisualsComponent comp, ProtoId<DamageGroupPrototype> group)
        => comp.DamageGroupColors.TryGetValue(group, out var color) ? color : null;

    private void SetLayerVisible(Entity<SpriteComponent?> sprite, int layer, bool visibility)
    {
        if (_sprite.TryGetLayer(sprite, layer, out var layerData, false) && layerData.Visible != visibility)
            _sprite.LayerSetVisible(sprite, layer, visibility);
    }

    private FixedPoint2 CalculateTotalBleeding(EntityUid uid)
    {
        var total = FixedPoint2.Zero;

        foreach (var wound in _wound.GetWoundableWounds(uid))
        {
            if (_bleedQuery.TryComp(wound, out var bleeds))
                total += bleeds.BleedingAmount;
        }

        return total;
    }

    private static BleedingSeverity CalculateBleedingThreshold(FixedPoint2 bleeding, WoundableVisualsComponent comp)
    {
        var nearestSeverity = BleedingSeverity.Minor;

        foreach (var (severity, value) in comp.BleedingThresholds)
        {
            if (bleeding < value)
                continue;

            nearestSeverity = severity;
            break;
        }

        return nearestSeverity;
    }

    private static FixedPoint2 GetThreshold(FixedPoint2 threshold, WoundableVisualsComponent comp)
    {
        var nearestSeverity = FixedPoint2.Zero;

        foreach (var value in comp.Thresholds)
        {
            if (threshold < value)
                continue;

            nearestSeverity = value;
            break;
        }

        return nearestSeverity;
    }

    private void UpdateBleedingLayerState(Entity<SpriteComponent?> sprite,
        int spriteLayer,
        string statePrefix,
        FixedPoint2 damage,
        BleedingSeverity threshold)
    {
        if (!_spriteQuery.Resolve(sprite, ref sprite.Comp))
            return;

        if (damage <= 0)
        {
            SetLayerVisible(sprite, spriteLayer, false);
            return;
        }

        SetLayerVisible(sprite, spriteLayer, true);

        if (_sprite.LayerGetEffectiveRsi(sprite, spriteLayer) is not {} rsi)
            return;

        var state = $"{statePrefix}_{threshold}";
        if (_random.Prob(AltBleedingSpriteChance))
            state += "_alt";

        if (rsi.TryGetState(state, out _))
            _sprite.LayerSetRsiState(sprite, spriteLayer, state);
    }

    private void UpdateDamageLayerState(Entity<SpriteComponent?> sprite,
        int spriteLayer,
        string statePrefix,
        FixedPoint2 threshold)
    {
        if (threshold <= 0)
        {
            _sprite.LayerSetVisible(sprite, spriteLayer, false);
        }
        else
        {
            if (!_sprite.TryGetLayer(sprite, spriteLayer, out var layer, false) || !layer.Visible)
                _sprite.LayerSetVisible(sprite, spriteLayer, true);
            _sprite.LayerSetRsiState(sprite, spriteLayer, $"{statePrefix}_{threshold}");
        }
    }

    private static string GetLimbBleedingKey(BodyPartComponent bodyPart)
    {
        var symmetry = bodyPart.Symmetry == BodyPartSymmetry.Left ? "L" : "R";
        // TODO SHITMED: Foot ? Leg : Arm - WHAT THE FUCK!?!?
        var partType = bodyPart.PartType == BodyPartType.Foot ? "Leg" : "Arm";
        return $"{symmetry}{partType}";
    }

    private static string BuildLayerKey(Enum baseLayer, string suffix) => $"{baseLayer}{suffix}";
    private static string BuildLayerKey(string baseLayer, string suffix) => $"{baseLayer}{suffix}";
    private static string BuildStateKey(Enum baseLayer, string suffix) => $"{baseLayer}_{suffix}";
    private static string BuildStateKey(Enum baseLayer, string group, string suffix) => $"{baseLayer}_{group}_{suffix}";

    #endregion
}
