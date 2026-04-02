// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Shaders;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Goobstation.Client.Shaders;

public sealed class MultiShaderSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new MultiShaderSpriteOverlay());

        SubscribeLocalEvent<SpriteComponent, SetMultiShaderEvent>(OnShader);
        SubscribeLocalEvent<SpriteComponent, SetMultiShadersEvent>(OnShaders);

        SubscribeLocalEvent<MultiShaderSpriteComponent, GetMultiShadersEvent>(OnGetShaders);
        SubscribeLocalEvent<MultiShaderSpriteComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<MultiShaderSpriteComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.RenderTarget?.Dispose();
    }

    private void OnGetShaders(Entity<MultiShaderSpriteComponent> ent, ref GetMultiShadersEvent args)
    {
        args.PostShaders = ent.Comp.PostShaders;
    }

    public void OnShaders(Entity<SpriteComponent> ent, ref SetMultiShadersEvent args)
    {
        if (!args.Add)
        {
            if (!TryComp(ent, out MultiShaderSpriteComponent? multi))
                return;

            if (args.PostShaders != null)
            {
                foreach (var proto in args.PostShaders.Keys)
                {
                    multi.PostShaders.Remove(proto);
                }
            }

            UpdateMultiShaderComp((ent, multi));
            return;
        }

        var comp = EnsureComp<MultiShaderSpriteComponent>(ent);

        if (args.PostShaders != null)
        {
            foreach (var (proto, data) in args.PostShaders)
            {
                comp.PostShaders[proto] = data;
            }
        }
    }

    public void OnShader(Entity<SpriteComponent> ent, ref SetMultiShaderEvent args)
    {
        if (!args.Add)
        {
            if (!TryComp(ent, out MultiShaderSpriteComponent? multi))
                return;

            multi.PostShaders.Remove(args.Proto);

            UpdateMultiShaderComp((ent, multi));
            return;
        }

        var comp = EnsureComp<MultiShaderSpriteComponent>(ent);
        comp.PostShaders[args.Proto] = new MultiShaderData
        {
            Color = args.Modulate,
            RenderOrder = args.RenderOrder,
            Mutable = args.Mutable,
            RaiseShaderEvent = args.RaiseEvent,
        };
    }

    private void UpdateMultiShaderComp(Entity<MultiShaderSpriteComponent> ent)
    {
        if (ent.Comp.PostShaders.Count == 0)
            RemCompDeferred(ent, ent.Comp);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<MultiShaderSpriteOverlay>();
    }
}
