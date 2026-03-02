// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Client.Shaders;
using Content.Shared._Shitcode.Heretic.Components;
using Robust.Client.GameObjects;

namespace Content.Goobstation.Client.Heretic;

public sealed class ShadowCloakSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowCloakEntityComponent, BeforePostMultiShaderRenderEvent>(RelayShader);
    }

    private void RelayShader(Entity<ShadowCloakEntityComponent> ent, ref BeforePostMultiShaderRenderEvent args)
    {
        if (!Exists(ent.Comp.User) || !TryComp(ent.Comp.User.Value, out SpriteComponent? sprite))
            return;

        RaiseLocalEvent(ent.Comp.User.Value, ref args);
    }
}
