// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns and attaches a part from the body's prototype, to this body part entity.
/// Organs do not get regenerated.
/// </summary>
public sealed partial class RegeneratePart : EntityEffectBase<RegeneratePart>
{
    /// <summary>
    /// The part slot to regenerate.
    /// It must exist on this part and in the body prototype.
    /// </summary>
    [DataField(required: true)]
    public string Slot = string.Empty;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-regenerate-part", ("chance", Probability), ("slot", Slot));
}

public sealed class RegeneratePartEffectSystem : EntityEffectSystem<BodyPartComponent, RegeneratePart>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    private EntityQuery<BodyComponent> _bodyQuery;

    public override void Initialize()
    {
        base.Initialize();

        _bodyQuery = GetEntityQuery<BodyComponent>();
    }

    protected override void Effect(Entity<BodyPartComponent> ent, ref EntityEffectEvent<RegeneratePart> args)
    {
        var slot = args.Effect.Slot;
        if (!ent.Comp.Children.ContainsKey(slot)) // slot doesn't exist on this part
            return;

        if (ent.Comp.Body is not {} body) // this part isn't attached to a body
            return;

        if (_bodyQuery.CompOrNull(body)?.Prototype is not {} protoId) // the body has no prototype (borg?)
            return;

        var proto = _proto.Index(protoId);
        if (!proto.Slots.TryGetValue(slot, out var slotDef)) // slot doesn't exist on the body prototype
            return;

        var child = PredictedSpawnNextToOrDrop(slotDef.Part, body);
        _body.AttachPart(ent, slot, child, ent.Comp);
    }
}
