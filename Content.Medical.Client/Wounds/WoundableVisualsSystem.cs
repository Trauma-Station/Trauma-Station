// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Medical.Common.Body;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Medical.Client.Wounds;

/// <summary>
/// Handles visual representation of wounds and damage on body parts
/// </summary>
public sealed class WoundableVisualsSystem : VisualizerSystem<WoundableVisualsComponent>
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private EntityQuery<VisualOrganComponent> _visualQuery;

    private const float AltBleedingSpriteChance = 0.15f;
    private const string BleedingSuffix = "Bleeding";
    private const string MinorSuffix = "Minor";

    public override void Initialize()
    {
        base.Initialize();

        _visualQuery = GetEntityQuery<VisualOrganComponent>();

        SubscribeLocalEvent<WoundableVisualsComponent, ComponentInit>(InitializeEntity, after: [typeof(WoundSystem)]);
        SubscribeLocalEvent<WoundableVisualsComponent, OrganGotRemovedEvent>(OnWoundableRemoved);
        SubscribeLocalEvent<WoundableVisualsComponent, OrganGotInsertedEvent>(OnWoundableInserted);
        SubscribeLocalEvent<WoundableVisualsComponent, WoundableIntegrityChangedEvent>(OnWoundableIntegrityChanged);
    }

    private Enum? GetLayer(EntityUid uid)
        => _visualQuery.CompOrNull(uid)?.Layer;

    private void InitializeEntity(Entity<WoundableVisualsComponent> ent, ref ComponentInit args)
    {
        InitDamage(ent);
        InitBleeding(ent);
    }

    private void InitBleeding(Entity<WoundableVisualsComponent> ent)
    {
        if (ent.Comp.BleedingOverlay is not {} overlay ||
            GetLayer(ent) is not {} layer)
            return;

        AddDamageLayerToSprite(ent.Owner, overlay, BuildStateKey(layer, MinorSuffix), BuildLayerKey(layer, BleedingSuffix));
    }

    private void InitDamage(Entity<WoundableVisualsComponent> ent)
    {
        if (GetLayer(ent) is not {} layer)
            return;

        foreach (var (group, sprite) in ent.Comp.DamageGroupSprites)
        {
            var color = GetColor(ent, group);
            AddDamageLayerToSprite(ent.Owner,
                sprite,
                BuildStateKey(layer, group, "100"),
                BuildLayerKey(layer, group),
                color);
        }
    }

    #region Event Handlers

    private void OnWoundableInserted(Entity<WoundableVisualsComponent> ent, ref OrganGotInsertedEvent args)
    {
        var body = args.Target;
        if (!HasComp<HumanoidProfileComponent>(body) ||
            !TryComp<SpriteComponent>(body, out var sprite) ||
            GetLayer(ent) is not {} layer)
            return;

        if (ent.Comp.DamageGroupSprites != null)
        {
            foreach (var (group, rsiPath) in ent.Comp.DamageGroupSprites)
            {
                if (SpriteSystem.LayerMapTryGet((body, sprite), BuildLayerKey(layer, group), out _, false))
                    continue;

                var color = GetColor(ent, group);
                AddDamageLayerToSprite((body, sprite),
                    rsiPath,
                    BuildStateKey(layer, group, "100"),
                    BuildLayerKey(layer, group),
                    color);
            }
        }

        if (!SpriteSystem.LayerMapTryGet((body, sprite), BuildLayerKey(layer, BleedingSuffix), out _, false)
            && ent.Comp.BleedingOverlay is {} overlay)
        {
            AddDamageLayerToSprite((body, sprite),
                overlay,
                BuildStateKey(layer, MinorSuffix),
                BuildLayerKey(layer, BleedingSuffix));
        }

        UpdateWoundableVisuals(ent, (body, sprite));
    }

    private void OnWoundableRemoved(Entity<WoundableVisualsComponent> ent, ref OrganGotRemovedEvent args)
    {
        var body = args.Target;
        if (!TryComp<SpriteComponent>(body, out var sprite))
            return;

        foreach (var part in _body.GetOrgans<WoundableVisualsComponent>(body.AsNullable()))
        {
            RemoveWoundableLayers(body.Owner, part);
            UpdateWoundableVisuals(part, (body, sprite));
        }
    }

    private void OnWoundableIntegrityChanged(Entity<WoundableVisualsComponent> ent, ref WoundableIntegrityChangedEvent args)
    {
        if (_body.GetBody(ent.Owner) is {} body)
            UpdateWoundableVisuals(ent, body);
        else
            UpdateWoundableVisuals(ent, ent.Owner); // use part's sprite
    }
    #endregion

    #region Layer Management
    private void RemoveWoundableLayers(Entity<SpriteComponent?> ent, Entity<WoundableVisualsComponent> visuals)
    {
        if (!Resolve(ent, ref ent.Comp) || GetLayer(visuals) is not {} partLayer)
            return;

        foreach (var (group, _) in visuals.Comp.DamageGroupSprites)
        {
            var layerKey = BuildLayerKey(partLayer, group);
            if (!SpriteSystem.LayerMapTryGet(ent, layerKey, out var layer, false))
                continue;

            SpriteSystem.LayerSetVisible(ent, layer, false);
            SpriteSystem.RemoveLayer(ent, layer);
            SpriteSystem.LayerMapRemove(ent, layerKey);
        }

        var bleedingKey = BuildLayerKey(partLayer, BleedingSuffix);
        if (!SpriteSystem.LayerMapTryGet(ent, bleedingKey, out var bleedLayer, false))
            return;

        SpriteSystem.LayerSetVisible(ent, bleedLayer, false);
        SpriteSystem.RemoveLayer(ent, bleedLayer, out _, false);
        SpriteSystem.LayerMapRemove(ent, bleedingKey, out _);
    }

    private void AddDamageLayerToSprite(Entity<SpriteComponent?> ent,
        string sprite,
        string state,
        string mapKey,
        Color? color = null)
    {
        if (!Resolve(ent, ref ent.Comp) || SpriteSystem.LayerMapTryGet(ent, mapKey, out _, false)) // prevent dupes
            return;

        var newLayer = SpriteSystem.AddLayer(ent,
            new SpriteSpecifier.Rsi(
                new ResPath(sprite),
                state
            ));
        SpriteSystem.LayerMapSet(ent, mapKey, newLayer);
        if (color != null)
            SpriteSystem.LayerSetColor(ent, newLayer, color.Value);
        SpriteSystem.LayerSetVisible(ent, newLayer, false);
    }
    #endregion

    #region Visual Updates
    private void UpdateWoundableVisuals(Entity<WoundableVisualsComponent> visuals, Entity<SpriteComponent?> sprite)
    {
        if (!Resolve(sprite, ref sprite.Comp))
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
            if (!SpriteSystem.LayerMapTryGet(sprite, $"{layer}{group.Key}", out var damageLayer, false))
                continue;
            var severityPoint = _wound.GetWoundableSeverityPoint(visuals, damageGroup: group.Key);
            UpdateDamageLayerState(sprite,
                damageLayer,
                $"{layer}_{group.Key}",
                severityPoint <= visuals.Comp.Thresholds.FirstOrDefault() ? 0 : GetThreshold(severityPoint, visuals));
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
        if (!TryComp<BodyPartComponent>(woundable, out var part) ||
            _part.GetParentPart(woundable.Owner) is not {} parent)
            return;

        var partKey = GetLimbBleedingKey(part);
        var layerKey = BuildLayerKey(partKey, BleedingSuffix);
        var hasWounds = TryGetWoundData(woundable.AsNullable(), out var wounds);
        var hasParentWounds = TryGetWoundData(parent, out var parentWounds);

        if (!hasWounds && !hasParentWounds)
        {
            if (SpriteSystem.LayerMapTryGet(sprite, layerKey, out var layer, false))
                SpriteSystem.LayerSetVisible(sprite, layer, false);
            return;
        }

        var totalBleeds = FixedPoint2.Zero;
        if (hasWounds)
            totalBleeds += CalculateTotalBleeding(wounds);
        if (hasParentWounds)
            totalBleeds += CalculateTotalBleeding(parentWounds);

        if (!SpriteSystem.LayerMapTryGet(sprite, layerKey, out var bleedingLayer, false))
            return;

        var threshold = CalculateBleedingThreshold(totalBleeds, woundable.Comp);
        UpdateBleedingLayerState(sprite, bleedingLayer, partKey, totalBleeds, threshold);
    }

    private void UpdateOwnBleedingVisuals(Entity<WoundableVisualsComponent> woundable, Entity<SpriteComponent?> sprite)
    {
        if (GetLayer(woundable) is not {} partLayer)
            return;

        var layerKey = BuildLayerKey(partLayer, BleedingSuffix);

        if (!TryGetWoundData(woundable.AsNullable(), out var wounds))
        {
            if (SpriteSystem.LayerMapTryGet(sprite, layerKey, out var layer, false))
                SpriteSystem.LayerSetVisible(sprite, layer, false);
            return;
        }

        var totalBleeds = CalculateTotalBleeding(wounds);
        if (!SpriteSystem.LayerMapTryGet(sprite, layerKey, out var bleedingLayer, false))
            return;
        var threshold = CalculateBleedingThreshold(totalBleeds, woundable.Comp);
        UpdateBleedingLayerState(sprite, bleedingLayer, partLayer.ToString(), totalBleeds, threshold);
    }

    #endregion
    #region Helper Methods
    private Color? GetColor(WoundableVisualsComponent comp, ProtoId<DamageGroupPrototype> group)
        => comp.DamageGroupColors.TryGetValue(group, out var color) ? color : null;

    private void SetLayerVisible(Entity<SpriteComponent?> sprite, int layer, bool visibility)
    {
        if (SpriteSystem.TryGetLayer(sprite, layer, out var layerData, false) && layerData.Visible != visibility)
            SpriteSystem.LayerSetVisible(sprite, layer, visibility);
    }

    private bool TryGetWoundData(Entity<WoundableVisualsComponent?> entity, [NotNullWhen(true)] out WoundVisualizerGroupData? wounds)
    {
        wounds = null;
        if (!Resolve(entity, ref entity.Comp, false) || !AppearanceSystem.TryGetData(entity.Owner, WoundableVisualizerKeys.Wounds, out wounds))
            return false;
        if (wounds.GroupList.Count != 0)
            return true;
        wounds = null;
        return false;
    }

    private FixedPoint2 CalculateTotalBleeding(params WoundVisualizerGroupData?[] woundGroups)
    {
        var total = FixedPoint2.Zero;

        foreach (var group in woundGroups)
        {
            if (group == null || group.GroupList.Count == 0)
                continue;

            foreach (var wound in group.GroupList)
            {
                if (TryComp<BleedInflicterComponent>(GetEntity(wound), out var bleeds))
                    total += bleeds.BleedingAmount;
            }
        }

        return total;
    }

    // TODO SHITMED: just have it as a sorted array what the fuck
    private static BleedingSeverity CalculateBleedingThreshold(FixedPoint2 bleeding, WoundableVisualsComponent comp)
    {
        var nearestSeverity = BleedingSeverity.Minor;

        foreach (var (severity, value) in comp.BleedingThresholds.OrderByDescending(kv => kv.Value))
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

        foreach (var value in comp.Thresholds.OrderByDescending(kv => kv.Value))
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
        if (!Resolve(sprite, ref sprite.Comp))
            return;

        if (damage <= 0)
        {
            SetLayerVisible(sprite, spriteLayer, false);
            return;
        }

        SetLayerVisible(sprite, spriteLayer, true);

        if (SpriteSystem.LayerGetEffectiveRsi(sprite, spriteLayer) is not {} rsi)
            return;

        var state = $"{statePrefix}_{threshold}";
        if (_random.Prob(AltBleedingSpriteChance))
            state += "_alt";

        if (rsi.TryGetState(state, out _))
            SpriteSystem.LayerSetRsiState(sprite, spriteLayer, state);
    }

    private void UpdateDamageLayerState(Entity<SpriteComponent?> sprite,
        int spriteLayer,
        string statePrefix,
        FixedPoint2 threshold)
    {
        if (threshold <= 0)
            SpriteSystem.LayerSetVisible(sprite, spriteLayer, false);
        else
        {
            if (!SpriteSystem.TryGetLayer(sprite, spriteLayer, out var layer, false) || !layer.Visible)
                SpriteSystem.LayerSetVisible(sprite, spriteLayer, true);
            SpriteSystem.LayerSetRsiState(sprite, spriteLayer, $"{statePrefix}_{threshold}");
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
