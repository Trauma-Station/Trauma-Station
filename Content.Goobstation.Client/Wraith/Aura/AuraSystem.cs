// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Graphics;
using Content.Goobstation.Shared.Wraith.Aura;

namespace Content.Goobstation.Client.Wraith.Aura;

/// <summary>
/// This be handling your aura 🥀
/// </summary>
public sealed partial class AuraSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "Aura";
    private static readonly ProtoId<ShaderPrototype> SecondSkinShader = "SecondSkin";

    private static readonly string[] AfterShaders =
    {
        SecondSkinShader.Id
    };

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
        _sprite.SetPostShader(ent.Owner,
            new(Shader, _shader)
            {
                RaiseShaderEvent = true,
                Before = ContentPostShaderIds.BeforeOutlines,
                After = AfterShaders,
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
        if (args.Id != Shader)
            return;

        args.Shader.SetParameter("distortion", ent.Comp.Distortion);
        args.Shader.SetParameter("auraColor",
            new Vector3(ent.Comp.AuraColor.R, ent.Comp.AuraColor.G, ent.Comp.AuraColor.B));
        args.Shader.SetParameter("mango", ent.Comp.AuraFarm);
    }
}
