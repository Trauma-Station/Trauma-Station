// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Trauma.Shared.Mind;

/// <summary>
/// System for seeing if someone is truly evil.
/// Unrelated to validhood.
/// </summary>
public sealed class EvilSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EvilComponent, IsEvilEvent>(Evil);
        SubscribeLocalEvent<MindContainerComponent, IsEvilEvent>(OnMindIsEvil);
    }

    public bool IsEvil(EntityUid target)
    {
        var ev = new IsEvilEvent(target);
        RaiseLocalEvent(target, ref ev);
        return ev.Evil;
    }

    private void Evil(Entity<EvilComponent> ent, ref IsEvilEvent args)
    {
        args.Evil = true;
    }

    private void OnMindIsEvil(Entity<MindContainerComponent> ent, ref IsEvilEvent args)
    {
        if (_mind.TryGetMind(ent, out var mind, out _, ent.Comp))
            RaiseLocalEvent(mind, ref args);
    }
}

/// <summary>
/// Event raised on a mob and its mind to see if it's evil.
/// </summary>
[ByRefEvent]
public record struct IsEvilEvent(EntityUid Target, bool Evil = false);
