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

        SubscribeLocalEvent<HeadRevolutionaryComponent, MapInitEvent>(OnHeadInit);
    }

    private void OnRevInit(Entity<RevolutionaryComponent> ent, ref MapInitEvent args)
    {
        // no potential conflict issue with headrev because it uses the highest value, 20 won't override 50 but 50 will override 20.
        EnsureKnowledge(ent, 20);
    }

    private void OnRevShutdown(Entity<RevolutionaryComponent> ent, ref ComponentShutdown args)
    {
        // covers both rev and headrev
        _knowledge.RemoveKnowledge(ent.Owner, RevolutionaryKnowledge);
    }

    private void OnHeadInit(Entity<HeadRevolutionaryComponent> ent, ref MapInitEvent args)
    {
        EnsureKnowledge(ent, 50);
    }

    private void EnsureKnowledge(EntityUid uid, int level)
    {
        if (_knowledge.GetContainer(uid) is not { } brain)
            return;

        _knowledge.EnsureKnowledge(brain, RevolutionaryKnowledge, level, popup: false); // no popup, it's obvious and clashes with other stuff probably
    }
}
