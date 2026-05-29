// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Hands;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.Input;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using System.Linq;
using System.Text;

namespace Content.Trauma.Client.ItemSlotRenderer;

/// <summary>
/// I can feel my grip on reality slowly slipping.
/// </summary>
public sealed partial class ItemSlotRendererSystem : EntitySystem
{
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private ItemSlotsSystem _slot = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemSlotRendererComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ItemSlotRendererComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ItemSlotRendererComponent, EntInsertedIntoContainerMessage>(OnInsertIntoContainer);
        SubscribeLocalEvent<ItemSlotRendererComponent, EntRemovedFromContainerMessage>(OnRemoveFromContainer);

    }
    private void OnInsertIntoContainer(EntityUid uid, ItemSlotRendererComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container is not ContainerSlot || !_timing.IsFirstTimePredicted)
            return;

        comp.CachedEntities[args.Container.ID] = args.Entity;
    }

    private void OnRemoveFromContainer(EntityUid uid, ItemSlotRendererComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container is not ContainerSlot || !_timing.IsFirstTimePredicted)
            return;

        comp.CachedEntities[args.Container.ID] = null;
    }

    private void OnRemove(EntityUid uid, ItemSlotRendererComponent comp, ComponentRemove args)
    {
        foreach (var (_, renderTexture) in comp.CachedRT)
            renderTexture.Dispose();
    }

    private void OnStartup(EntityUid uid, ItemSlotRendererComponent comp, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            Log.Error($"ItemSlotRendererComponent requires SpriteComponent to work, but {ToPrettyString(uid)} did not have one. Removing ItemSlotRenderer.");
            RemComp(uid, comp);
            return;
        }

        foreach (var (slotId, mapKey) in comp.PrototypeLayerMappings)
        {
            var layer = 0;
            var ent = (uid, sprite);
            if (_reflection.TryParseEnumReference(mapKey, out var e))
            {
                if (!_sprite.LayerMapTryGet(ent, e, out layer, comp.ErrorOnMissing))
                {
                    if (comp.ErrorOnMissing)
                        Log.Error($"{ToPrettyString(uid)}: Tried to add a missing layer under the enum key {mapKey}. Skipping missing layer. If this is unwanted, set component's ErrorOnMissing to false.");
                    continue;
                }
            }
            else if (!_sprite.LayerMapTryGet(ent, mapKey, out layer, comp.ErrorOnMissing))
            {
                if (comp.ErrorOnMissing)
                    Log.Error($"{ToPrettyString(uid)}: Tried to add a missing layer under the string key {mapKey}. Skipping missing layer. If this is unwanted, set component's ErrorOnMissing to false.");
                continue;
            }

            if (_slot.TryGetSlot(uid, slotId, out var slot))
                comp.CachedEntities[slotId] = slot.Item;

            comp.LayerMappings.Add((layer, slotId));

            comp.CachedRT.Add(slotId, _clyde.CreateRenderTarget(comp.RenderTargetSize,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                new TextureSampleParameters { Filter = false },
                $"{slotId}-itemrender-rendertarget"));
        }
    }
}

/// <summary>
/// Doesn't actually render anything by itself. I'd place this code in a system's FrameUpdate,
/// but I need to somehow acquire a draw handle to draw an entity to a texture.
/// </summary>
public sealed partial class SpriteToLayerBullshitOverlay : Overlay
{
    [Dependency] private EntityManager _ent = default!;
    private SpriteSystem? _sprite;

    public override OverlaySpace Space => OverlaySpace.ScreenSpaceBelowWorld;

    public SpriteToLayerBullshitOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _sprite ??= _ent.System<SpriteSystem>();

        var handle = args.ScreenHandle;
        var query = _ent.EntityQueryEnumerator<ItemSlotRendererComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            var ent = (uid, sprite);
            for (int i = 0; i < comp.LayerMappings.Count; i++)
            {
                var (index, slotId) = comp.LayerMappings[i];
                if (!_sprite.TryGetLayer(ent, index, out var layer, true)) // verify that the layer actually exists
                    continue;

                // if for some reason we can't render the item to a texture (or there is no item to render),
                // assign an "empty" texture to the layer
                if (!comp.CachedEntities.TryGetValue(slotId, out var _item) || _item is not EntityUid item ||
                    !comp.CachedRT.TryGetValue(slotId, out var renderTarget))
                {
                    if (layer.Texture != Texture.Transparent)
                        _sprite.LayerSetTexture(layer, Texture.Transparent);
                    continue;
                }

                handle.RenderInRenderTarget(renderTarget, () =>
                {
                    handle.DrawEntity(item, renderTarget.Size / 2, Vector2.One, 0); // If this throws due to a missing spritecomp, it's your fault.
                }, Color.Transparent);
                _sprite.LayerSetTexture(layer, renderTarget.Texture);
            }
        }
    }
}
