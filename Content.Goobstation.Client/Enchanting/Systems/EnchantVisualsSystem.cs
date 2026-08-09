// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Enchanting.Components;
using Content.Goobstation.Shared.Enchanting.Systems;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using System.Linq;

namespace Content.Goobstation.Client.Enchanting.Systems;

/// <summary>
/// Gives enchanted items a cool shader
/// </summary>
public sealed partial class EnchantVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public readonly ProtoId<ShaderPrototype> Shader = "Enchant";
    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = ProtoMan.Index(Shader).Instance();
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<EnchantedComponent> ent, ref ComponentStartup args)
    {
        _sprite.SetPostShader(ent.Owner, new(Shader, _shader));
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<EnchantedComponent> ent, ref ComponentShutdown args)
    {
        if (!Terminating(ent))
            _sprite.RemovePostShader(ent.Owner, Shader);
    }

    [SubscribeLocalEvent]
    private void OnHeldVisualsUpdated(Entity<EnchantedComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        SetLayers(args.User, args.RevealedLayers);
    }

    [SubscribeLocalEvent]
    private void OnEquipmentVisualsUpdated(Entity<EnchantedComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        SetLayers(args.Equipee, args.RevealedLayers);
    }

    [SubscribeLocalEvent]
    private void OnEnchanterHandleState(Entity<EnchanterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_sprite.TryGetLayer(ent.Owner, EnchanterVisuals.Layer, out var layer, false))
            _sprite.LayerSetVisible(layer, ent.Comp.Enchants.Count > 0);
    }

    private void SetLayers(EntityUid uid, HashSet<string> keys)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var ent = (uid, sprite);
        foreach (var key in keys)
        {
            if (_sprite.TryGetLayer(ent, key, out var layer, true))
                layer.Shader = _shader;
        }
    }
}
