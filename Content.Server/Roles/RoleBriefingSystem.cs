using Content.Shared.Roles.Components;

namespace Content.Server.Roles;

public sealed class RoleBriefingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleBriefingComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void OnGetBriefing(EntityUid uid, RoleBriefingComponent comp, ref GetBriefingEvent args)
    {
        args.Append(Loc.TryGetString(comp.Briefing, out var briefing) ? briefing : comp.Briefing); // Trauma - use TryGetString, some systems set this to a localized string
    }
}
