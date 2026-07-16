// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Research.Systems;
using Content.Trauma.Shared.AER;

namespace Content.Trauma.Server.AER;


public sealed partial class AnomalousEntityContainmentSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalousEntityComponent, AerBehaviourAddResearchEvent>(OnAerBehaviourResearch);
    }

    /// <summary>
    /// Add a research tantum on AerBehaviourResearchEvents
    /// </summary>
    private void OnAerBehaviourResearch(Entity<AnomalousEntityComponent> ent, ref AerBehaviourAddResearchEvent args)
    {
        if (ent.Comp.ConnectedContainment is not { } containment)
            return;
        if (!_research.TryGetClientServer(containment, out var server, out var serverComponent))
            return;

        if (server != null && ent.Comp.Contained)
        {
            _research.ModifyServerPoints(server.Value, ent.Comp.ResearchOnBehaviour, serverComponent);
        }
    }
}
