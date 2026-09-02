// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Systems;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Trauma.Server.Objectives.Components;
using Content.Trauma.Server.StationEvents.Components;
using Content.Trauma.Shared.Roles;

namespace Content.Trauma.Server.Objectives.Systems;

public sealed partial class FugitiveTargetSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FugitiveTargetComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<FugitiveTargetComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAssigned(EntityUid uid, FugitiveTargetComponent comp, ref ObjectiveAssignedEvent args)
    {
        // Find the active fugitive rule
        var ruleQuery = EntityQueryEnumerator<FugitiveRuleComponent>();
        if (!ruleQuery.MoveNext(out _, out var rule))
        {
            args.Cancelled = true;
            return;
        }

        if (Prototype(uid) is not { } proto)
        {
            args.Cancelled = true;
            return;
        }

        var objectiveProto = new EntProtoId(proto.ID);

        // Reuse stored target so both hunters always get the same objective
        if (rule.ObjectiveTargets.TryGetValue(objectiveProto, out var storedTarget))
        {
            _target.SetTarget(uid, storedTarget);
            return;
        }

        // First hunter: pick a fugitive not already used by another objective
        var taken = new HashSet<EntityUid>(rule.ObjectiveTargets.Values);
        var fugitiveMind = FindAvailableFugitiveMind(taken);

        if (fugitiveMind == null)
        {
            args.Cancelled = true;
            return;
        }

        rule.ObjectiveTargets[objectiveProto] = fugitiveMind.Value;
        _target.SetTarget(uid, fugitiveMind.Value);
    }

    private void OnAfterAssign(EntityUid uid, FugitiveTargetComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        if (!_target.GetTarget(uid, out var mindUid))
            return;

        var description = GetFugitiveDescription(mindUid.Value);
        var currentName = Name(uid);

        // Append the description to the objective name, example: "Eliminate the fugitive (young human male)"
        _metaData.SetEntityName(uid, $"{currentName} ({description})", args.Meta);
    }

    /// <summary>
    /// Returns a short physical description of the fugitive, example: "young human male"
    /// </summary>
    private string GetFugitiveDescription(EntityUid mindUid)
    {
        if (!TryComp<MindComponent>(mindUid, out var mind) || mind.OwnedEntity is not {} mob)
            return Loc.GetString("fugitive-objective-unknown");

        if (!TryComp<HumanoidProfileComponent>(mob, out var humanoid))
            return Name(mob);

        var speciesProto = ProtoMan.Index(humanoid.Species);
        var species = Loc.GetString(speciesProto.Name).ToLower();
        var sex = humanoid.Sex.ToString().ToLower();

        var age = humanoid.Age switch
        {
            var a when a < speciesProto.YoungAge => Loc.GetString("fugitive-objective-age-young"),
            var a when a < speciesProto.OldAge  => Loc.GetString("fugitive-objective-age-middle"),
            _                                   => Loc.GetString("fugitive-objective-age-old"),
        };

        // pick DNA first, fall back to fingerprint, fall back to nothing
        string identifier;
        if (TryComp<DnaComponent>(mob, out var dna) && dna.DNA != null)
            identifier = Loc.GetString("fugitive-objective-identifier-dna", ("dna", dna.DNA));
        else if (TryComp<FingerprintComponent>(mob, out var prints) && prints.Fingerprint != null)
            identifier = Loc.GetString("fugitive-objective-identifier-prints", ("prints", prints.Fingerprint));
        else
            identifier = string.Empty;

        var baseDescription = Loc.GetString("fugitive-objective-description",
            ("age", age), ("species", species), ("sex", sex));

        return identifier == string.Empty
            ? baseDescription
            : $"{baseDescription} — {identifier}";
    }

    /// <summary>
    /// Finds a fugitive mind not already assigned to another objective
    /// </summary>
    private EntityUid? FindAvailableFugitiveMind(HashSet<EntityUid> taken)
    {
        var query = EntityQueryEnumerator<FugitiveRoleComponent>();
        while (query.MoveNext(out var roleUid, out _))
        {
            var mindUid = Transform(roleUid).ParentUid;
            if (!taken.Contains(mindUid))
                return mindUid;
        }
        return null;
    }
}
