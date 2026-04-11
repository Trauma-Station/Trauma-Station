// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Wizard.SanguineStrike;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.Wizard.Systems;

public sealed class SanguineStrikeSystem : SharedSanguineStrikeSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SanguineStrikeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SanguineStrikeComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<SanguineStrikeComponent> ent, ref ComponentShutdown args)
    {
        var (uid, comp) = ent;

        if (TerminatingOrDeleted(uid))
            return;

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        sprite.Color = comp.OldColor;
    }

    private void OnStartup(Entity<SanguineStrikeComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        comp.OldColor = sprite.Color;
        sprite.Color = comp.Color;
    }
}
