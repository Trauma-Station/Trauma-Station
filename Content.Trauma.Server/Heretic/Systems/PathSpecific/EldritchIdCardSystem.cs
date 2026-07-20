// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Access.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Lock;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Lock;

namespace Content.Trauma.Server.Heretic.Systems.PathSpecific;

public sealed class EldritchIdCardSystem : SharedEldritchIdCardSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EldritchIdCardComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<EldritchIdCardComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Comp.PortalOne))
            QueueDel(ent.Comp.PortalOne);

        if (!TerminatingOrDeleted(ent.Comp.PortalTwo))
            QueueDel(ent.Comp.PortalTwo);
    }

    protected override bool InitializeEldritchId(Entity<EldritchIdCardComponent> ent)
    {
        if (!base.InitializeEldritchId(ent))
            return false;

        RemCompDeferred<AgentIDCardComponent>(ent);
        return true;
    }
}
