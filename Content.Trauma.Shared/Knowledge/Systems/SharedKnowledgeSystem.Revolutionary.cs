// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Revolutionary.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    private static readonly EntProtoId RevolutionaryKnowledge = "RevolutionaryKnowledge";
    void InitializeRevolutionaries()
    {
        SubscribeLocalEvent<RevolutionaryComponent, ComponentStartup>(OnRevStartup);
        SubscribeLocalEvent<HeadRevolutionaryComponent, ComponentStartup>(OnHeadRevStartup);
    }

    private void OnRevStartup(Entity<RevolutionaryComponent> ent, ref ComponentStartup args)
    {
        if (GetContainer(ent) is { } brain)
            EnsureKnowledge(brain, RevolutionaryKnowledge, 26);
    }

    private void OnHeadRevStartup(Entity<HeadRevolutionaryComponent> ent, ref ComponentStartup args)
    {
        if (GetContainer(ent) is { } brain)
            EnsureKnowledge(brain, RevolutionaryKnowledge, 40);
    }
}
