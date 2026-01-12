using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

public sealed partial class HumanoidAppearanceSystem
{
    private bool TryGetParentMarking(MarkingPrototype markingPrototype,
        HumanoidAppearanceComponent humanoid,
        [NotNullWhen(true)] out SpriteSpecifier.Rsi? rsi,
        [NotNullWhen(true)] out string? layerId)
    {
        rsi = null;
        layerId = null;

        var category = MarkingCategoriesConversion.FromHumanoidVisualLayers(markingPrototype.BodyPart);
        var marking = humanoid.MarkingSet.Markings.GetValueOrDefault(category)?.FirstOrDefault();

        if (marking == null || !_markingManager.Markings.TryGetValue(marking.MarkingId, out var proto))
            return false;

        rsi = proto.Sprites.FirstOrDefault() as SpriteSpecifier.Rsi;

        if (rsi == null)
            return false;

        layerId = $"{proto.ID}-{rsi.RsiState}";
        return true;
    }

    private void TryRemoveParentShader(MarkingPrototype prototype, Entity<HumanoidAppearanceComponent, SpriteComponent> ent)
    {
        var humanoid = ent.Comp1;
        var spriteComp = ent.Comp2;

        if (prototype.Sprites.Count == 0 && prototype.Shader != null &&
            TryGetParentMarking(prototype, humanoid, out _, out var id) &&
            _sprite.LayerMapTryGet((ent, spriteComp), id, out var ind, false))
            spriteComp.LayerSetShader(ind, null, null);
    }

    private void TryApplyParentShader(MarkingPrototype markingPrototype,
        int targetLayer,
        Entity<HumanoidAppearanceComponent, SpriteComponent> entity)
    {
        if (markingPrototype.Sprites.Count != 0 || markingPrototype.Shader is not { } shaderProto)
            return;

        var humanoid = entity.Comp1;
        var sprite = entity.Comp2;
        Entity<SpriteComponent?> spriteEnt = (entity, sprite);

        var shader = _prototypeManager.Index<ShaderPrototype>(shaderProto).InstanceUnique();
        if (markingPrototype.Coloring.Layers is { } layers)
        {
            foreach (var (key, def) in layers)
            {
                shader.SetParameter(key,
                    def.GetColor(humanoid.SkinColor, humanoid.EyeColor, humanoid.MarkingSet).RGBA);
            }
        }

        if (!TryGetParentMarking(markingPrototype, humanoid, out var rsi, out var layerId))
            sprite.LayerSetShader(targetLayer, shader, shaderProto);
        else
        {
            if (!_sprite.LayerMapTryGet(spriteEnt, layerId, out var layer, false))
            {
                layer = _sprite.AddLayer(spriteEnt, rsi, targetLayer + 1);
                _sprite.LayerMapSet(spriteEnt, layerId, layer);
            }
            sprite.LayerSetShader(layerId, shader, shaderProto);
        }
    }
}
