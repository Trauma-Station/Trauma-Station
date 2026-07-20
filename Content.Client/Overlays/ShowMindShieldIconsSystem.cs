using Content.Shared.Mindshield.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Overlays;

public sealed partial class ShowMindShieldIconsSystem : EquipmentHudSystem<ShowMindShieldIconsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindShieldComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        SubscribeLocalEvent<FakeMindShieldComponent, GetStatusIconsEvent>(OnGetStatusIconsEventFake);
    }
    // TODO: Probably need to get this OFF of client since this can be read by bad actors rather easily
    //  ...imagine cheating in a game about silly paper dolls
    private void OnGetStatusIconsEventFake(EntityUid uid, FakeMindShieldComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;
        if (component.IsEnabled && ProtoMan.Resolve(component.MindShieldStatusIcon, out var fakeStatusIconPrototype))
            ev.StatusIcons.Add(fakeStatusIconPrototype);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, MindShieldComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        // <Trauma> - different icon when broken
        var icon = component.Broken ? component.MindShieldBrokenStatusIcon : component.MindShieldStatusIcon;
        if (ProtoMan.Resolve(icon, out var iconPrototype))
        // </Trauma>
            ev.StatusIcons.Add(iconPrototype);
    }
}
