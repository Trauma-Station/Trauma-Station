// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Server.GameStates;

namespace Content.Trauma.Server.PvsOverride;

public sealed partial class ForcePvsOverrideSystem : EntitySystem
{
    [Dependency] private PvsOverrideSystem _pvs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForcePvsOverrideComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ForcePvsOverrideComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<ForcePvsOverrideComponent> ent, ref ComponentShutdown args)
    {
        _pvs.RemoveGlobalOverride(ent);
    }

    private void OnStartup(Entity<ForcePvsOverrideComponent> ent, ref ComponentStartup args)
    {
        _pvs.AddGlobalOverride(ent);
    }
}
