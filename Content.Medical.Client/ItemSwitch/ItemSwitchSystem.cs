// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Client.Items;
using Content.Medical.Shared.ItemSwitch;
using Robust.Client.GameObjects;

namespace Content.Medical.Client.ItemSwitch;

public sealed class ItemSwitchSystem : SharedItemSwitchSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<ItemSwitchComponent>(ent => new ItemSwitchStatusControl(ent));
        SubscribeLocalEvent<ItemSwitchComponent, AfterAutoHandleStateEvent>(OnChanged);
    }

    private void OnChanged(Entity<ItemSwitchComponent> ent, ref AfterAutoHandleStateEvent args) => UpdateVisuals(ent, ent.Comp.State);

    protected override void UpdateVisuals(Entity<ItemSwitchComponent> ent, string key)
    {
        base.UpdateVisuals(ent, key);
        if (ent.Comp.States.TryGetValue(key, out var state) &&
            state.Sprite is {} sprite)
            _sprite.LayerSetSprite(ent.Owner, 0, sprite);
    }
}
