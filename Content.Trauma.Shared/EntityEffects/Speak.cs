using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.EntityEffects;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Makes the target entity say a random line from a localized dataset.
/// </summary>
public sealed partial class Speak : EntityEffect
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Id;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var proto = _proto.Index(Id);
        var picked = _random.Pick(proto);
        var uid = args.TargetEntity;
        // this is still logged so admins can know e.g. what started a dispute, it would look bad say
        // if you say fuck 8 times to pun pun and he starts attacking you
        // vs you say nothing for 30s and pun pun randomly attacks you according to evil logs
        _chat.TrySendInGameICMessage(uid, picked, InGameICChatType.Speak, hideChat: false);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-speak");
}
