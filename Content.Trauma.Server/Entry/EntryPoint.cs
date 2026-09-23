// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Server.IoC;
using Robust.Shared.ContentPack;

namespace Content.Trauma.Server.Entry;

public sealed partial class EntryPoint : GameServer
{
    [Dependency] private IComponentFactory _factory = default!;

    public override void PreInit()
    {
        ServerTraumaIoC.Register(Dependencies);
    }

    public override void Init()
    {
        base.Init();

        Dependencies.InjectDependencies(this);

        _factory.RegisterIgnore(IgnoredComponents);
    }

    private static readonly string[] IgnoredComponents =
    [
        "RotationDrawDepth",
        "ToggleableLightWieldable",
        "HideClothingLayerClothing",
        "ItemSlotRenderer",
        "ShowSpriteLayerStatusEffect",
        "AnimatedEmotesBlacklist",
        "PredictedPhysicsEffect",
        "TimedClientDespawn",
    ];
}
