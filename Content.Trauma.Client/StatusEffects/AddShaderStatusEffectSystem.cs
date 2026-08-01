// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.StatusEffects;

namespace Content.Trauma.Client.StatusEffects;

public sealed partial class AddShaderStatusEffectSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<AddShaderStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        var id = ent.Comp.Shader;
        var shader = ProtoMan.Index<ShaderPrototype>(id).Instance();
        var data = new SpriteComponent.PostShaderArgs(id, shader)
        {
            GetScreenTexture = true,
            RaiseShaderEvent = true
        };
        _sprite.SetPostShader(args.Target, data);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<AddShaderStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!Terminating(args.Target))
            _sprite.RemovePostShader(args.Target, ent.Comp.Shader);
    }
}
