// <Trauma>
using Content.Trauma.Common.Mindshield;
using Content.Shared.Revolutionary; // GoobStation
using Content.Server.Revolutionary.Components; // GoobStation
// </Trauma>
using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Robust.Shared.Containers;

namespace Content.Server.Mindshield;

/// <summary>
/// System used for adding or removing components with a mindshield implant
/// as well as checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    //[Dependency] private readonly PopupSystem _popupSystem = default!; // Trauma - unused now
    [Dependency] private readonly SharedRevolutionarySystem _revolutionary = default!; // Goobstation

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindShieldImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<MindShieldImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
    }

    private void OnImplantImplanted(Entity<MindShieldImplantComponent> ent, ref ImplantImplantedEvent ev)
    {
        EnsureComp<MindShieldComponent>(ev.Implanted);
        MindShieldRemovalCheck(ev.Implanted, ev.Implant);

        // GoobStation
        if (TryComp<CommandStaffComponent>(ev.Implanted, out var commandComp))
            commandComp.Enabled = true;
    }

    /// <summary>
    /// Checks if the implanted person was a Rev or Head Rev and remove role or destroy mindshield respectively.
    /// </summary>
    private void MindShieldRemovalCheck(EntityUid implanted, EntityUid implant)
    {
        // <Trauma>
        var ev = new RemoveMindShieldEvent();
        RaiseLocalEvent(implanted, ref ev);
        if (ev.Cancelled)
            return;
        // </Trauma>

        if (_mindSystem.TryGetMind(implanted, out var mindId, out _) &&
            _roleSystem.MindRemoveRole<RevolutionaryRoleComponent>(mindId))
        {
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(implanted)} was deconverted due to being implanted with a Mindshield.");
        }
        if (HasComp<Goobstation.Shared.Mindcontrol.MindcontrolledComponent>(implanted))   //Goobstation - Mindcontrol Implant
            RemComp<Goobstation.Shared.Mindcontrol.MindcontrolledComponent>(implanted);
    }

    private void OnImplantRemoved(Entity<MindShieldImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        // <Goob>
        // FIXME: should only be shown when removed by implanter not polymorph etc fuck sake
        //_popupSystem.PopupEntity(Loc.GetString("mindshield-implant-effect-removed"), args.Implanted, args.Implanted);

        if (TryComp<HeadRevolutionaryComponent>(args.Implanted, out var headRevComp))
            _revolutionary.SetConvertAbility((args.Implanted, headRevComp), true);
        // </Goob>

        RemComp<MindShieldComponent>(args.Implanted);
    }
}
