// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item;
using Content.Trauma.Shared.Forging;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Trauma.Client.Forging;

/// <summary>
/// Sets procgen forged item sprites.
/// </summary>
public sealed class ForgingVisualsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly EntityQuery<ItemComponent> _itemQuery = default!;
    [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForgedItemComponent, ComponentStartup>(OnForgedStartup);
        SubscribeLocalEvent<SpriteComponent, ForgingCompletedEvent>(OnSpriteForged);
    }

    private void OnForgedStartup(Entity<ForgedItemComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Completed)
            UpdateSprites(ent.Owner, _proto.Index(ent.Comp.Item));
    }

    private void OnSpriteForged(Entity<SpriteComponent> ent, ref ForgingCompletedEvent args)
    {
        UpdateSprites(ent.AsNullable(), args.Item);
    }

    private void UpdateSprites(Entity<SpriteComponent?> ent, ForgedItemPrototype proto)
    {
        if (!_spriteQuery.Resolve(ent, ref ent.Comp))
            return;

        if (proto.Sprite is not {} sprite)
            return;

        var path = SpriteSpecifierSerializer.TextureRoot / sprite;
        var rsi = _cache.GetResource<RSIResource>(path).RSI;
        _sprite.SetBaseRsi(ent, rsi);
        if (_itemQuery.TryComp(ent, out var item))
            item.RsiPath = sprite.ToString();
    }
}
