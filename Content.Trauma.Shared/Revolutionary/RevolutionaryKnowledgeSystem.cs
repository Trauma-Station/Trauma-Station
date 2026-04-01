// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Revolutionary.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Revolutionary;

/// <summary>
/// Ensures revs start with revolutionary knowledge, and lose it if deconverted.
/// </summary>
public sealed class RevolutionaryKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public static readonly EntProtoId RevolutionaryKnowledge = "RevolutionaryKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        // TODO: need to update these if chuds ever make it a mind-only component
        SubscribeLocalEvent<RevolutionaryComponent, MapInitEvent>(OnRevInit);
        SubscribeLocalEvent<RevolutionaryComponent, ComponentShutdown>(OnRevShutdown);
    }

    private void OnRevInit(Entity<RevolutionaryComponent> ent, ref MapInitEvent args)
    {
        if (_knowledge.GetContainer(ent) is not { } brain)
            return;

        _knowledge.EnsureKnowledge(brain, RevolutionaryKnowledge, 100, popup: false); // no popup, it's obvious and clashes with other stuff probably
    }

    private void OnRevShutdown(Entity<RevolutionaryComponent> ent, ref ComponentShutdown args)
    {
        // covers both rev and headrev
        _knowledge.RemoveKnowledge(ent.Owner, RevolutionaryKnowledge);
    }
}
