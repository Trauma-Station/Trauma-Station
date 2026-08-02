// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Wraith.Aura;

namespace Content.Goobstation.Client.Wraith.Aura;

/// <summary>
/// This be handling your aura 🥀
/// </summary>
public sealed partial class AuraSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "Aura";

    private ShaderInstance _shader = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _shader = ProtoMan.Index(Shader).InstanceUnique();
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<AuraComponent> ent, ref ComponentStartup args)
    {
        _sprite.SetPostShader(ent.Owner, new(Shader, _shader)
        {
            GetScreenTexture = true,
            RaiseShaderEvent = true
        });
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<AuraComponent> ent, ref ComponentShutdown args)
    {
        if (!Terminating(ent.Owner))
            _sprite.RemovePostShader(ent.Owner, Shader);
    }

    [SubscribeLocalEvent]
    private void OnShaderRender(Entity<AuraComponent> ent, ref BeforePostShaderRenderEvent args)
    {
        if (args.Sprite.PostShader != _shader)
            return;

        _shader.SetParameter("distortion", ent.Comp.Distortion);
        _shader.SetParameter("auraColor", new Vector3(ent.Comp.AuraColor.R, ent.Comp.AuraColor.G, ent.Comp.AuraColor.B));
        _shader.SetParameter("mango", ent.Comp.AuraFarm);
    }
}
