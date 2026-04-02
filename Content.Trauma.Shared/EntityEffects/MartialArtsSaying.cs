// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Chat;
using Content.Shared.EntityEffects;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class MartialArtsSaying : EntityEffectBase<MartialArtsSaying>
{
    /// <summary>
    /// The list of sayings.
    /// </summary>
    [DataField(required: true)]
    public string[] RandomSayings = default!;

    /// <summary>
    /// The list of while downed.
    /// </summary>
    [DataField(required: true)]
    public string[] RandomSayingsDowned = default!;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null; // idc
}

public sealed class MartialArtsSayingEffectSystem : EntityEffectSystem<TransformComponent, MartialArtsSaying>
{
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;


    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<MartialArtsSaying> args)
    {
        var saying = "martial-arts-saying-generic";
        if (_standing.IsDown(ent.Owner))
        {
            saying = _random.Pick(args.Effect.RandomSayingsDowned);
        }
        else
        {
            saying = _random.Pick(args.Effect.RandomSayings);
        }
        _chat.TrySendInGameICMessage(ent, Loc.GetString(saying), InGameICChatType.Speak, false);
    }
}
