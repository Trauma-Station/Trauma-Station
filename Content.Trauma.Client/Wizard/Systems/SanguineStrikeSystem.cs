// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Wizard.SanguineStrike;

namespace Content.Trauma.Client.Wizard.Systems;

public sealed partial class SanguineStrikeSystem : SharedSanguineStrikeSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SanguineStrikeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SanguineStrikeComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<SanguineStrikeComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        comp.OldColor = sprite.Color;
        _sprite.SetColor((uid, sprite), comp.Color);
    }

    private void OnShutdown(Entity<SanguineStrikeComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _sprite.SetColor(ent.Owner, ent.Comp.OldColor);
    }

}
