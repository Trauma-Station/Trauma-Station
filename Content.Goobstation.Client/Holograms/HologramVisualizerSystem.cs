// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Holograms;

namespace Content.Goobstation.Client.Holograms;

public sealed partial class HologramVisualizerSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly ProtoId<ShaderPrototype> _shaderId = "Holographic";
    private ShaderInstance _shader = default!;

    public override void Initialize()
    {
        base.Initialize();

        _shader = ProtoMan.Index(_shaderId).Instance();
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<HologramVisualsComponent> ent, ref ComponentInit args)
    {
        _sprite.SetPostShader(ent.Owner, new(_shaderId, _shader));
    }

    [SubscribeLocalEvent]
    private void OnComponentShutdown(Entity<HologramVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            _sprite.RemovePostShader(ent.Owner, _shaderId);
    }
}
