// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Lumminal <81829924+Lumminal@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Roudenn <romabond091@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Shadowling;

public sealed class EnthrallOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _shader;
    private double _startTime = -1;
    private double _lastsFor = 1;

    public static readonly ProtoId<ShaderPrototype> EnthrallEffect = "EnthrallEffect";

    public EnthrallOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(EnthrallEffect).Instance().Duplicate();
    }

    public void ReceiveEnthrall(double duration)
    {
        _startTime = _timing.CurTime.TotalSeconds;
        _lastsFor = duration;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var percentComplete = (float) ((_timing.CurTime.TotalSeconds - _startTime) / _lastsFor);
        if (percentComplete >= 1.0f)
            return;

        var worldHandle = args.WorldHandle;
        _shader.SetParameter("percentComplete", percentComplete);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entMan.TryGetComponent(_player.LocalEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return true;
    }
}
