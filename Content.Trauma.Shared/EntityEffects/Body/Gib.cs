// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Gibbing.Components;
using Content.Shared.Gibbing.Events;
using Content.Shared.Gibbing.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Gibs the target mob or gibbable (body part).
/// </summary>
public sealed partial class Gib : EntityEffectBase<Gib>
{
    [DataField]
    public bool GibOrgans;

    [DataField]
    public bool LaunchGibs = true;

    [DataField]
    public GibType GibType = GibType.Gib;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-gib", ("chance", Probability));
}

public sealed class GibEffectSystem : EntityEffectSystem<TransformComponent, Gib>
{
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    private HashSet<EntityUid> _dropped = new();
    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<BodyPartComponent> _partQuery;
    private EntityQuery<GibbableComponent> _gibbableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _bodyQuery = GetEntityQuery<BodyComponent>();
        _partQuery = GetEntityQuery<BodyPartComponent>();
        _gibbableQuery = GetEntityQuery<GibbableComponent>();
    }

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<Gib> args)
    {
        var effect = args.Effect;
        if (_bodyQuery.TryComp(ent, out var body))
        {
            _body.GibBody(ent,
                effect.GibOrgans,
                body,
                effect.LaunchGibs,
                gib: effect.GibType);
            return;
        }

        if (!_gibbableQuery.TryComp(ent, out var gibbable)) // bad target, neither a mob nor bodypart
            return;

        var outer = _partQuery.TryComp(ent, out var part) && part.Body is {} bodyUid
            ? (bodyUid, null)
            : ent.AsNullable();
        _dropped.Clear();
        var contents = GibContentsOption.Drop;
        _gibbing.TryGibEntity(outer, (ent, gibbable), effect.GibType,
            contents, out _dropped, effect.LaunchGibs);
    }
}
