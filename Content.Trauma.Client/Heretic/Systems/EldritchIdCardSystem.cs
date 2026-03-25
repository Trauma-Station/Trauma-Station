// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Lock;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed class EldritchIdCardSystem : SharedEldritchIdCardSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EldritchIdCardComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<EldritchIdCardComponent> ent, ref ComponentStartup args)
    {
        UpdateSprite(ent);
    }

    protected override void UpdateSprite(Entity<EldritchIdCardComponent> ent)
    {
        if (ent.Comp.CurrentProto == null)
            return;

        var dummy = Spawn(ent.Comp.CurrentProto);
        _sprite.CopySprite(dummy, ent.Owner);
        Del(dummy);
    }
}
