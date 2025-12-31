using Content.Goobstation.Common.Barks;
using Content.Goobstation.Common.CCVar;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Goobstation.Shared.Barks;

public sealed class BarkSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechSynthesisComponent, EntitySpokeEvent>(OnEntitySpoke);
        Subs.CVar(_cfg, GoobCVars.BarksEnabled, x => _enabled = x, true);
    }

    private void OnEntitySpoke(EntityUid uid, SpeechSynthesisComponent comp, EntitySpokeEvent args)
    {
        if (comp.VoicePrototypeId is null
            || !args.Language.SpeechOverride.RequireSpeech
            || !_enabled)
            return;

        var sourceEntity = GetNetEntity(uid);
        RaiseNetworkEvent(new PlayBarkEvent(sourceEntity, args.Message, args.IsWhisper), Filter.Pvs(uid));
    }
}
