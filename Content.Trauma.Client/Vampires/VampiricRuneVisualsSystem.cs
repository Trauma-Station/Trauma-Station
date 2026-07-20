// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Vampires.Lair;

namespace Content.Trauma.Client.Vampires;

public sealed partial class VampiricRuneVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampiricRuneVisualsComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnAppearance(Entity<VampiricRuneVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !_appearance.TryGetData<Color>(ent, VampiricRuneVisuals.Color, out var color))
            return;

        _sprite.SetColor((ent.Owner, args.Sprite), color);
    }
}
