using System.Linq;
using Content.Shared.FixedPoint;
using Content.Server._EinsteinEngines.Language;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared._EinsteinEngines.Language;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared._White.Xenomorphs.Xenomorph;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._White.Xenomorphs.Xenomorph;

public sealed class XenomorphSystem : SharedXenomorphSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly WoundSystem _wounds = default!; // Goobstation
    [Dependency] private readonly BodySystem _body = default!; // Goobstation
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    public override void Update(float frameTime)
    {
        // Goobstation start
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<XenomorphComponent, BloodstreamComponent, BodyComponent>();  // Added BodyComponent to query

        while (query.MoveNext(out var uid, out var xenomorph, out var bloodstream, out var body))
        {
            if (xenomorph.WeedHeal == null || time < xenomorph.NextPointsAt)
                continue;

            // Update next heal time
            xenomorph.NextPointsAt = time + xenomorph.WeedHealRate;

            if (!xenomorph.OnWeed)
                continue;

            // Apply regular weed healing if on weeds
            _damageable.TryChangeDamage(uid, xenomorph.WeedHeal);

            // Process bleeding and blood loss in parallel with cached values
            ProcessBloodLoss(uid, bloodstream);
        }
    }

    // Slowly heal bloodloss
    private void ProcessBloodLoss(EntityUid uid, BloodstreamComponent bloodstream)
    {
        if (_bloodstream.GetBloodLevel((uid, bloodstream)) > 0.99f)
            return;

        _bloodstream.TryModifyBloodLevel((uid, bloodstream), 0.5);
        var bloodloss = new DamageSpecifier();
        bloodloss.DamageDict["Bloodloss"] = -0.2f;  // Heal blood per tick
        _damageable.TryChangeDamage(uid, bloodloss);
    }
    // Goobstation end

    private void OnEntitySpoke(EntityUid uid, XenomorphComponent component, EntitySpokeEvent args)
    {
        if (args.Source != uid || args.Language.ID != component.XenoLanguageId || args.IsWhisper)
            return;

        SendMessage(args.Source, args.Message, false, args.Language);
    }

    private bool CanHearXenoHivemind(EntityUid entity, string languageId)
    {
        var understood = _language.GetUnderstoodLanguages(entity);
        return understood.Any(language => language.Id == languageId);
    }

    private void SendMessage(EntityUid source, string message, bool hideChat, LanguagePrototype language)
    {
        var clients = GetClients(language.ID);
        var playerName = Name(source);
        var wrappedMessage = Loc.GetString(
            "chat-manager-send-xeno-hivemind-chat-wrap-message",
            ("channelName", Loc.GetString("chat-manager-xeno-hivemind-channel-name")),
            ("player", playerName),
            ("message", FormattedMessage.EscapeText(message)));

        _chatManager.ChatMessageToMany(
            ChatChannel.Telepathic,
            message,
            wrappedMessage,
            source,
            hideChat,
            true,
            clients.ToList(),
            language.SpeechOverride.Color);
    }

    private IEnumerable<INetChannel> GetClients(string languageId) =>
        Filter.Empty()
            .AddWhereAttachedEntity(entity => CanHearXenoHivemind(entity, languageId))
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
}
