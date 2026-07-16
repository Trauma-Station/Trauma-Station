// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Guardian.Components;
using Content.Shared.Polymorph;
using Robust.Shared.Containers;

namespace Content.Trauma.Server.Guardian;

/// <summary>
/// Transfers holoparasites when polymorphed.
/// </summary>
/// <remarks>
/// We love bespoke shitcode system only used by 1 thing and barely integrated to the rest of the game
/// </remarks>
public sealed partial class GuardianPolymorphSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuardianComponent, PolymorphedEvent>(OnPolymorphed);
        SubscribeLocalEvent<GuardianHostComponent, PolymorphedEvent>(OnHostPolymorphed);
    }

    private void OnPolymorphed(Entity<GuardianComponent> ent, ref PolymorphedEvent args)
    {
        if (ent.Owner != args.OldEntity || ent.Comp.Host is not { } host ||
            !TryComp<GuardianHostComponent>(host, out var hostComp))
            return;

        var comp = EnsureComp<GuardianComponent>(args.NewEntity);
        comp.Host = host;
        ent.Comp.Host = null; // dont let removing it below clean up the host's action etc
        hostComp.HostedGuardian = args.NewEntity;
        RemComp(ent, ent.Comp);
        // polymorph system is trusted to swap the new entity into the container
    }

    private void OnHostPolymorphed(Entity<GuardianHostComponent> ent, ref PolymorphedEvent args)
    {
        if (ent.Owner != args.OldEntity ||
            ent.Comp.HostedGuardian is not { } guardian ||
            EnsureComp<GuardianHostComponent>(args.NewEntity, out var comp)) // dont know what to do if the new entity already has a holo...
            return;

        // new entity owns the guardian now
        ent.Comp.HostedGuardian = null;
        comp.HostedGuardian = guardian;
        // transfer to the new container if it's not deployed
        if (ent.Comp.GuardianContainer.Contains(guardian))
            _container.Insert(guardian, comp.GuardianContainer, force: true);
        // guardian belongs to the new entity now
        Comp<GuardianComponent>(guardian).Host = args.NewEntity;
        RemComp(ent, ent.Comp); // detach it from the old entity completely
    }
}
