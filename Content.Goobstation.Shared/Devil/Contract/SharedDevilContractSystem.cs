// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Devil.Condemned;
using Content.Goobstation.Shared.Possession;
using Content.Shared.Examine;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Mindshield.Components;
using Content.Shared.Nutrition;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Goobstation.Shared.Devil.Contract;

public abstract class SharedDevilContractSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public static readonly EntityWhitelist SoulBlacklist = new()
    {
        Components =
        [
            "ChangelingIdentity", // not a person
            "Condemned", // already sold it
            // robots
            "BorgChassis",
            "Drone",
            "Silicon"
        ]
    };

    protected readonly Dictionary<LocId, Func<DevilContractComponent, EntityUid?>> _targetResolvers = new()
    {
        // The contractee is who is making the deal.
        ["devil-contract-contractee"] = comp => comp.Signer,
        // The contractor is the entity offering the deal.
        ["devil-contract-contractor"] = comp => comp.ContractOwner,
    };

    protected Regex _clauseRegex = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevilContractComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<DevilContractComponent, SignAttemptEvent>(OnSignAttempt);
        SubscribeLocalEvent<DevilContractComponent, PaperSignedEvent>(OnSigned);
        SubscribeLocalEvent<DevilContractComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DevilContractComponent, FullyEatenEvent>(OnFullyEaten);

        SubscribeLocalEvent<DevilComponent, GetSignatureEvent>(OnGetSignature);

        InitializeRegex();
    }

    private void InitializeRegex()
    {
        var escapedPatterns = _targetResolvers.Keys.Select(locId => Loc.GetString(locId)).ToList(); // malicious linq and regex
        var targetPattern = string.Join("|", escapedPatterns);

        _clauseRegex = new Regex($@"^\s*(?<target>{targetPattern})\s*:\s*(?<clause>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
    }

    #region Event handlers

    private void OnGetVerbs(Entity<DevilContractComponent> contract, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (!args.CanInteract || !args.CanAccess || !TryComp<DevilComponent>(user, out var devil))
            return;

        AlternativeVerb burnVerb = new()
        {
            Act = () => TryBurnContract(contract, (user, devil)),
            Text = Loc.GetString("burn-contract-prompt"),
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Effects/fire.rsi"), "fire"),
        };

        args.Verbs.Add(burnVerb);
    }

    private void OnSignAttempt(Entity<DevilContractComponent> contract, ref SignAttemptEvent args)
    {
        // Make sure that weight is set properly!
        UpdateContractWeight(contract);

        // Don't allow mortals to sign contracts for other people.
        var user = args.Signer;
        if (contract.Comp.IsVictimSigned && user != contract.Comp.ContractOwner)
        {
            var invalidUserPopup = Loc.GetString("devil-sign-invalid-user");
            _popup.PopupClient(invalidUserPopup, contract, user);

            args.Cancelled = true;
            return;
        }

        // Only handle unsigned contracts.
        if (contract.Comp.IsVictimSigned || contract.Comp.IsDevilSigned)
            return;

        if (!IsUserValid(user, out var failReason))
        {
            _popup.PopupClient(failReason, contract, user, PopupType.MediumCaution);

            args.Cancelled = true;
            return;
        }

        // Check if the weight is too low
        if (!contract.Comp.IsContractSignable)
        {
            var difference = Math.Abs(contract.Comp.ContractWeight);

            var unevenOddsPopup = Loc.GetString("contract-uneven-odds", ("number", difference));
            _popup.PopupClient(unevenOddsPopup, contract, user, PopupType.MediumCaution);

            args.Cancelled = true;
            return;
        }

        // Check if devil is trying to sign first
        if (user == contract.Comp.ContractOwner)
        {
            var tooEarlyPopup = Loc.GetString("devil-contract-early-sign-failed");
            _popup.PopupClient(tooEarlyPopup, contract, user, PopupType.MediumCaution);

            args.Cancelled = true;
        }
    }

    private void OnSigned(Entity<DevilContractComponent> contract, ref PaperSignedEvent args)
    {
        // Determine signing phase
        if (!contract.Comp.IsVictimSigned)
            HandleVictimSign(contract, args.User);
        else if (!contract.Comp.IsDevilSigned)
            HandleDevilSign(contract, args.User);

        // Final activation check
        if (contract.Comp.IsContractFullySigned)
            HandleBothPartiesSigned(contract);
    }

    private void OnExamined(Entity<DevilContractComponent> contract, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        UpdateContractWeight(contract);
        args.PushMarkup(Loc.GetString("devil-contract-examined", ("weight", contract.Comp.ContractWeight)));
    }

    private void OnFullyEaten(Entity<DevilContractComponent> contract, ref FullyEatenEvent args)
    {
        _explosion.QueueExplosion(
            args.User,
            typeId: "Default",
            totalIntensity: 1, // contract explosions should not cause any kind of major structural damage. you should at worst need to weld a window or repair a table.
            slope: 1,
            maxTileIntensity: 1,
            maxTileBreak: 0,
            addLog: false);
    }

    private void OnGetSignature(Entity<DevilComponent> ent, ref GetSignatureEvent args)
    {
        var name = ent.Comp.TrueName;
        if (!string.IsNullOrWhiteSpace(name))
            args.Signature = name;
    }

    #endregion

    public void TryBurnContract(Entity<DevilContractComponent> contract, Entity<DevilComponent> devil)
    {
        var coordinates = Transform(contract).Coordinates;

        if (contract.Comp.IsContractFullySigned)
        {
            _popup.PopupCoordinates(Loc.GetString("burn-contract-popup-fail"), coordinates, devil, PopupType.MediumCaution);
            return;
        }

        PredictedSpawnAtPosition(devil.Comp.FireEffectProto, coordinates);
        _audio.PlayPredicted(devil.Comp.FwooshPath, coordinates, devil, new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f));
        PredictedQueueDel(contract);
    }

    private void HandleVictimSign(Entity<DevilContractComponent> contract, EntityUid signer)
    {
        contract.Comp.Signer = signer;
        contract.Comp.IsVictimSigned = true;

        if (TryComp<PaperComponent>(contract, out var paper))
        {
            paper.EditingDisabled = true;
            Dirty(contract, paper);
        }

        _popup.PopupClient(Loc.GetString("contract-victim-signed"), contract, signer);
    }

    private void HandleDevilSign(Entity<DevilContractComponent> contract, EntityUid signer)
    {
        contract.Comp.IsDevilSigned = true;
        _popup.PopupClient(Loc.GetString("contract-devil-signed"), contract, signer);
        AdvanceObjective(signer, contract.Comp.ContractWeight);
    }

    private void HandleBothPartiesSigned(Entity<DevilContractComponent> contract)
    {
        UpdateContractWeight(contract);
        DoContractEffects(contract);
    }

    protected virtual void AdvanceObjective(EntityUid devil, int weight)
    {
        // server-side
    }

    protected virtual void DoContractEffects(Entity<DevilContractComponent> contract)
    {
        // server-side, could be predicted but i cbf
    }

    public bool TryTransferSouls(EntityUid devil, EntityUid contractee, int added)
    {
        // Can't sell what doesn't exist.
        if (HasComp<CondemnedComponent>(contractee)
            || devil == contractee)
            return false;

        var ev = new SoulAmountChangedEvent(devil, contractee, added);
        RaiseLocalEvent(devil, ref ev);

        var condemned = EnsureComp<CondemnedComponent>(contractee);
        condemned.SoulOwner = devil;
        condemned.CondemnOnDeath = true;
        Dirty(contractee, condemned);

        return true;
    }

    public void UpdateContractWeight(Entity<DevilContractComponent> contract)
    {
        if (!TryComp<PaperComponent>(contract, out var paper))
            return;

        contract.Comp.CurrentClauses.Clear();
        var newWeight = 0;

        var matches = _clauseRegex.Matches(paper.Content);
        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;

            var clauseKey = match.Groups["clause"].Value.Trim().ToLowerInvariant().Replace(" ", "");

            if (!Proto.TryIndex(clauseKey, out DevilClausePrototype? clauseProto)
                || !contract.Comp.CurrentClauses.Add(clauseProto))
                continue;

            newWeight += clauseProto.ClauseWeight;
        }

        contract.Comp.ContractWeight = newWeight;
        Dirty(contract);
    }

    public bool IsUserValid(EntityUid user, out string failReason)
    {
        if (_whitelist.IsWhitelistPass(SoulBlacklist, user))
        {
            failReason = Loc.GetString("devil-contract-no-soul-sign-failed");
            return false;
        }

        if (HasComp<MindShieldComponent>(user)
            && !HasComp<DevilComponent>(user))
        {
            failReason = Loc.GetString("devil-contract-mind-shielded-failed");
            return false;
        }

        if (HasComp<PossessedComponent>(user))
        {
            failReason = Loc.GetString("devil-contract-early-sign-failed");
            return false;
        }

        failReason = string.Empty;
        return true;
    }

}
