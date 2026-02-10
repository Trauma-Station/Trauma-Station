// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Access.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Trauma.Common.Paper;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Paper;

public sealed class SignatureSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    // The sprite used to visualize "signatures" on paper entities.
    private const string SignatureStampState = "paper_stamp-signature";

    public static readonly ProtoId<TagPrototype> PenTag = "Write";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(Entity<PaperComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using is not {} pen || !_tag.HasTag(pen, PenTag))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                TrySignPaper(ent, user, pen);
            },
            Text = Loc.GetString("paper-sign-verb"),
            DoContactInteraction = true,
            Priority = 10
        });
    }

    /// <summary>
    ///     Tries to add a signature to the paper with signer's name.
    /// </summary>
    public bool TrySignPaper(Entity<PaperComponent> paper, EntityUid signer, EntityUid pen)
    {
        var attemptEv = new SignAttemptEvent(signer);
        RaiseLocalEvent(paper, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        var signatureEv = new GetSignatureEvent(paper);
        RaiseLocalEvent(signer, ref signatureEv);
        var signatureName = signatureEv.Signature ?? GetDefaultSignature(signer);

        var stampInfo = new StampDisplayInfo()
        {
            StampedName = signatureName,
            StampedColor = Color.DarkSlateGray, // TODO Make this configurable depending on the pen.
        };

        if (!_paper.TryStamp(paper, stampInfo, SignatureStampState))
        {
            _popup.PopupEntity(Loc.GetString("paper-signed-failure", ("target", paper)), signer, signer, PopupType.SmallCaution);
            return false;
        }

        _audio.PlayPredicted(paper.Comp.Sound, signer, signer);

        _paper.UpdateUserInterface(paper);

        // devil has its own popup so let it cancel it
        var ev = new PaperSignedEvent(signer);
        RaiseLocalEvent(paper, ref ev);
        if (!ev.Handled)
        {
            var identity = Identity.Entity(signer, EntityManager);
            var you = Loc.GetString("paper-signed-self", ("target", paper));
            var others = Loc.GetString("paper-signed-other", ("user", identity), ("target", paper));
            _popup.PopupPredicted(you, others, signer, signer);
        }

        return true;
    }

    private string GetDefaultSignature(EntityUid uid)
    {
        // If the entity has an ID, use the name on it.
        if (_idCard.TryFindIdCard(uid, out var id) && !string.IsNullOrWhiteSpace(id.Comp.FullName))
            return id.Comp.FullName;

        // Alternatively, return the entity name
        return Name(uid);
    }
}
