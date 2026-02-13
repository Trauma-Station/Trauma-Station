// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Linq;
using System.Text;
using Content.Goobstation.Common.Examine;
using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.PartStatus;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Server.Chat.Managers;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.HealthExaminable;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Medical.Server.PartStatus;

public sealed class PartStatusSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private static readonly BodyPartType[] BodyPartOrder =
    [
        BodyPartType.Head,
        BodyPartType.Torso,
        BodyPartType.Arm,
        BodyPartType.Hand,
        BodyPartType.Leg,
        BodyPartType.Foot,
    ];

    private static List<BodyPartSymmetry> _symmetryPriority =
    [
        BodyPartSymmetry.Left,
        BodyPartSymmetry.Right,
        BodyPartSymmetry.None,
    ];

    private const string BleedLocaleStr = "inspect-wound-Bleeding-moderate";
    private const string BoneLocaleStr = "inspect-trauma-BoneDamage";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GetPartStatusEvent>(OnGetPartStatus);
        SubscribeLocalEvent<HealthExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetPartStatus(GetPartStatusEvent message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} entity ||
            _mob.IsIncapacitated(entity)) // fuck you i guess???
            return;

        var partStatusSet = CollectPartStatuses(entity);
        var text = GetExamineText(entity, entity, partStatusSet);

        _chat.ChatMessageToOne(
            ChatChannel.Emotes,
            text.ToMarkup(),
            text.ToMarkup(),
            EntityUid.Invalid,
            false,
            args.SenderSession.Channel,
            recordReplay: false);
    }


    private void OnGetExamineVerbs(EntityUid uid, HealthExaminableComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damage))
            return;

        var detailsRange = _examine.IsInDetailsRange(args.User, uid);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = CreateMarkup(uid, args.User, component, damage);
                _examine.SendExamineTooltip(args.User, uid, markup, false, false);
                var examineCompletedEvent = new ExamineCompletedEvent(markup, uid, args.User, true); // Goobstation
                RaiseLocalEvent(uid, examineCompletedEvent); // Goobstation
            },
            Text = Loc.GetString("health-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("health-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    public FormattedMessage CreateMarkup(EntityUid uid, EntityUid examiner, HealthExaminableComponent component, DamageableComponent damage)
    {
        var partStatusSet = CollectPartStatuses(uid);
        var text = GetExamineText(uid, examiner, partStatusSet, false);
        // Anything else want to add on to this?
        RaiseLocalEvent(uid, new HealthBeingExaminedEvent(text), true);

        return text;
    }


    private HashSet<PartStatus> CollectPartStatuses(EntityUid body)
    {
        var partStatusSet = new HashSet<PartStatus>();

        foreach (var woundable in _body.GetOrgans<WoundableComponent>(body))
        {
            if (!TryComp<BodyPartComponent>(woundable, out var part) ||
                _body.GetCategory(woundable.Owner) is not {} category)
                continue;

            var (damageSeverities, isBleeding) = AnalyzeWounds(woundable);
            var boneSev = _trauma.GetBone(woundable.AsNullable())?.Comp.BoneSeverity ?? BoneSeverity.Normal; // fallback for boneless limbs like slimes
            partStatusSet.Add(new PartStatus(
                part.PartType,
                part.Symmetry,
                _proto.Index(category).Name.ToLowerInvariant(), // looks better lowercase
                woundable.Comp.WoundableSeverity,
                damageSeverities,
                boneSev,
                isBleeding));
        }

        return partStatusSet;
    }

    private (Dictionary<string, WoundSeverity> DamageSeverities, bool IsBleeding) AnalyzeWounds(
        Entity<WoundableComponent> woundable)
    {
        var damageSeverities = new Dictionary<string, WoundSeverity>();
        var isBleeding = false;

        foreach (var wound in _wound.GetWoundableWounds(woundable))
        {
            if (wound.Comp.DamageGroup == null
                || wound.Comp.WoundSeverity == WoundSeverity.Healed)
                continue;

            if (!damageSeverities.TryGetValue(wound.Comp.DamageType, out var existingSeverity) ||
                wound.Comp.WoundSeverity > existingSeverity)
                damageSeverities[_proto.Index(wound.Comp.DamageGroup).LocalizedName] = wound.Comp.WoundSeverity;

            if (TryComp<BleedInflicterComponent>(wound, out var bleeds) && bleeds.IsBleeding)
                isBleeding = true;
        }

        return (damageSeverities, isBleeding);
    }

    private FormattedMessage GetExamineText(EntityUid entity,
        EntityUid examiner,
        HashSet<PartStatus> partStatusSet,
        bool styling = true)
    {
        var message = new FormattedMessage();
        var titlestring = entity == examiner
            ? "inspect-part-status-title"
            : "inspect-part-status-title-other";

        if (styling)
        {
            message.PushTag(new MarkupNode("examineborder", null, null)); // border
            message.PushNewline();
        }
        else
        {
            titlestring += "-styleless";
        }

        message.AddMarkupPermissive(Loc.GetString(titlestring, ("entity", FormattedMessage.EscapeText(Identity.Name(entity, EntityManager)))));
        message.PushNewline();
        AddLine(message);
        CreateBodyPartMessage(partStatusSet, entity == examiner, ref message, !styling);

        if (styling)
        {
            message.Pop();
            message.PushNewline();
        }

        return message;
    }

    private void CreateBodyPartMessage(HashSet<PartStatus> partStatusSet,
        bool inspectingSelf,
        ref FormattedMessage message,
        bool styleless = false)
    {
        var orderedParts = BodyPartOrder
            .SelectMany(partType => partStatusSet.Where(p => p.PartType == partType)
                .ToList()
                .OrderBy(p => _symmetryPriority.IndexOf(p.PartSymmetry)))
            .ToList();

        foreach (var partStatus in orderedParts)
        {
            var statusDescription = BuildStatusDescription(partStatus, inspectingSelf);
            var possessive = inspectingSelf
                ? Loc.GetString("inspect-part-status-you")
                : Loc.GetString("inspect-part-status-their");

            var locString = "inspect-part-status-line";

            if (styleless)
            {
                locString += "-styleless";
            }

            message.AddMarkupPermissive("    " + Loc.GetString(locString,
                ("possessive", possessive),
                ("part", partStatus.PartName),
                ("status", statusDescription)));

            message.PushNewline();
        }
    }

    private string BuildStatusDescription(PartStatus partStatus, bool inspectingSelf)
    {
        var sb = new StringBuilder();
        var hasStatus = false;

        // Get overall wound severity
        var overallSeverity = GetOverallWoundSeverity(partStatus.DamageSeverities);
        if (overallSeverity != WoundSeverity.Healed)
        {
            var localeText = $"inspect-wound-{overallSeverity.ToString().ToLower()}";
            sb.Append(Loc.GetString(localeText));
            hasStatus = true;
        }

        // Add damage group descriptions
        var damageDescriptions = GetDamageGroupDescriptions(partStatus.DamageSeverities, inspectingSelf);
        if (damageDescriptions.Count > 0)
        {
            if (hasStatus)
                sb.Append(Loc.GetString("inspect-part-status-comma"));
            sb.Append(Loc.GetString("inspect-part-status-conjunction"));
            sb.Append(string.Join(" ", damageDescriptions));
            hasStatus = true;
        }

        // Add trauma descriptions
        var traumaDescriptions = GetTraumaDescriptions(partStatus, inspectingSelf);
        if (traumaDescriptions.Count > 0)
        {
            if (hasStatus)
                sb.Append(Loc.GetString("inspect-part-status-conjunction2"));
            else
                sb.Append(Loc.GetString("inspect-part-status-conjunction3"));
            sb.Append(string.Join(Loc.GetString("inspect-part-status-comma"), traumaDescriptions));
            hasStatus = true;
        }

        if (!hasStatus)
            sb.Append(Loc.GetString("inspect-part-status-fine"));

        return sb.ToString();
    }

    private WoundSeverity GetOverallWoundSeverity(Dictionary<string, WoundSeverity> damageSeverities)
    {
        if (damageSeverities.Count == 0)
            return WoundSeverity.Healed;

        var maxSeverity = WoundSeverity.Healed;
        foreach (var (type, severity) in damageSeverities)
        {
            if (type is not ("Brute" or "Burn") // At some point we gonna de-hardcode this, but i doubt that day is soon.
                || severity <= maxSeverity)
                continue;

            maxSeverity = severity;
        }
        return maxSeverity;
    }

    private List<string> GetDamageGroupDescriptions(Dictionary<string, WoundSeverity> damageSeverities, bool inspectingSelf)
    {
        var descriptions = new List<string>();
        foreach (var (type, severity) in damageSeverities)
        {
            if (type is not ("Brute" or "Burn"))
                continue;

            var cappedSeverity = severity > WoundSeverity.Severe ? WoundSeverity.Severe : severity;
            var localeText = $"inspect-wound-{type}-{cappedSeverity.ToString().ToLower()}";
            descriptions.Add(Loc.GetString(localeText));
        }

        if (descriptions.Count > 1)
        {
            var lastDescription = descriptions[^1];
            descriptions[^1] = Loc.GetString("inspect-part-status-and") + lastDescription;
        }

        return descriptions;
    }

    private List<string> GetTraumaDescriptions(PartStatus partStatus, bool inspectingSelf)
    {
        var descriptions = new List<string>();

        // TODO: Dehardcode this guscode from bone traumas when we actually have more organ traumas.

        // Add bone trauma
        if (partStatus.BoneSeverity > BoneSeverity.Normal)
        {
            var localeText = inspectingSelf ? "self-inspect-trauma-BoneDamage" : "inspect-trauma-BoneDamage";
            descriptions.Add(Loc.GetString(localeText));
        }

        // Add bleeding status
        if (partStatus.Bleeding)
        {
            var localeText = "inspect-wound-Bleeding-moderate";
            descriptions.Add(Loc.GetString(localeText));
        }

        // If we have multiple traumas, add "and it" before the last one
        if (descriptions.Count > 1)
        {
            var lastDescription = descriptions[^1];
            descriptions[^1] = Loc.GetString("inspect-part-status-and") + lastDescription;
        }

        return descriptions;
    }

    private void AddLine(FormattedMessage message)
    {
        message.PushColor(Color.FromHex("#282D31"));
        message.AddText(Loc.GetString("examine-border-line"));
        message.PushNewline();
        message.Pop();
    }
}
