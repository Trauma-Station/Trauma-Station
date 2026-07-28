// <Trauma>
using Content.Trauma.Common.Language.Systems;
// </Trauma>
using Content.Shared.Abilities.Mime;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;
using Content.Shared.Puppet;
using Content.Shared.Speech.EntitySystems;

namespace Content.Shared.Speech.Muting;

/// <summary>
/// A system to prevent muted characters from talking.
/// </summary>
/// <seealso cref="MutedComponent"/>
public sealed partial class MutingSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private CommonLanguageSystem _languages = default!;
    // </Trauma>
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<MimePowersComponent> _mimePowersQuery;
    [Dependency] private EntityQuery<VentriloquistPuppetComponent> _puppetQuery;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutedComponent, EmoteEvent>(OnEmote, before: new[] { typeof(VocalSystem), typeof(MumbleAccentSystem) });
        SubscribeLocalEvent<MutedComponent, EmoteActionEvent>(OnEmoteAction, before: new[] { typeof(VocalSystem) });
    }

    private void OnEmote(Entity<MutedComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        //still leaves the text so it looks like they are pantomiming a laugh
        if (args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            args.Handled = true;
    }

    private void OnEmoteAction(Entity<MutedComponent> ent, ref EmoteActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ProtoMan.Resolve(args.Emote, out var emote))
            return;

        if (!emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        if (_mimePowersQuery.HasComp(ent))
            _popup.PopupEntity(Loc.GetString("mime-cant-speak"), ent, ent);
        else
            _popup.PopupEntity(Loc.GetString("speech-muted"), ent, ent);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnSpeakAttempt(Entity<MutedComponent> ent, ref SpeakAttemptEvent args)
    {
        // <Trauma>
        var language = _languages.GetLanguage(uid);
        if (!language.SpeechOverride.RequireSpeech)
            return; // Cannot mute if there's no speech involved
        // </Trauma>

        // TODO something better than this.
        if (_mimePowersQuery.HasComp(ent))
            _popup.PopupEntity(Loc.GetString("mime-cant-speak"), ent, ent);
        else if (_puppetQuery.HasComp(ent))
            _popup.PopupEntity(Loc.GetString("ventriloquist-puppet-cant-speak"), ent, ent);
        else
            _popup.PopupEntity(Loc.GetString("speech-muted"), ent, ent);

        args.Cancel();
    }
}
