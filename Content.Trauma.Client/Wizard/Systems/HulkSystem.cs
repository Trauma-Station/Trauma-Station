// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Shared.Wizard.Mutate;
using Content.Shared.Body;

namespace Content.Trauma.Client.Wizard.Systems;

public sealed partial class HulkSystem : SharedHulkSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HulkComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<HulkComponent> ent, ref ComponentShutdown args)
    {
        var (uid, comp) = ent;

        if (TerminatingOrDeleted(uid))
            return;

        if (HasComp<VisualBodyComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var layerCount = sprite.AllLayers.Count();

        if (comp.NonHumanoidOldLayerData.Count != layerCount)
            return;

        var spriteEnt = (uid, sprite);
        for (var i = 0; i < layerCount; i++)
        {
            _sprite.LayerSetColor(spriteEnt, i, comp.NonHumanoidOldLayerData[i]);
        }
    }

    protected override void UpdateColorStartup(Entity<HulkComponent> hulk)
    {
        base.UpdateColorStartup(hulk);

        var (uid, comp) = hulk;

        if (HasComp<VisualBodyComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var ent = (uid, sprite);
        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            if (!_sprite.TryGetLayer(ent, i, out var layer, false))
                continue;

            comp.NonHumanoidOldLayerData.Add(layer.Color);
            _sprite.LayerSetColor(ent, i, comp.SkinColor);
        }
    }
}
