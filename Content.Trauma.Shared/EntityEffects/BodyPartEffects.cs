using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Runs nested entity effects on all body parts of a given type.
/// </summary>
public sealed partial class BodyPartEffects : EntityEffect
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private SharedBodySystem? _body;

    /// <summary>
    /// The body part type to run effects on.
    /// It will run on all of them if there are multiple.
    /// </summary>
    [DataField(required: true)]
    public BodyPartType PartType;

    /// <summary>
    /// Optional part symmetry to require.
    /// </summary>
    [DataField]
    public BodyPartSymmetry? PartSymmetry;

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField(required: true)]
    public LocId GuidebookText;

    [DataField(required: true)]
    public List<EntityEffect> Effects;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString(GuidebookText);

    public override void Effect(EntityEffectBaseArgs args)
    {
        _body ??= args.EntityManager.System<SharedBodySystem>();

        var body = args.TargetEntity;
        foreach (var part in _body.GetBodyChildrenOfType(body, PartType, symmetry: PartSymmetry))
        {
            args.TargetEntity = part.Id;
            foreach (var effect in Effects)
            {
                if (effect.ShouldApply(args, _random))
                    effect.Effect(args);
            }
        }
    }
}
