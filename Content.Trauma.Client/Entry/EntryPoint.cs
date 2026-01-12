// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Client.Humanoid;
using Content.Trauma.Client.IoC;
using Robust.Shared.ContentPack;

namespace Content.Trauma.Client.Entry;

public sealed class EntryPoint : GameClient
{
    [Dependency] private readonly ShaderMarkingManager _shaderMarking = default!;


    public override void PreInit()
    {
        base.PreInit();

        ContentTraumaClientIoC.Register(Dependencies);
    }

    public override void Init()
    {
        base.Init();

        Dependencies.BuildGraph();
        Dependencies.InjectDependencies(this);
    }

    public override void PostInit()
    {
        base.PostInit();

        _shaderMarking.Initialize();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _shaderMarking.Shutdown();
    }
}
