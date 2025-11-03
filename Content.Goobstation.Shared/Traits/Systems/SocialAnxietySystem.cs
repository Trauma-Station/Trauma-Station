using Content.Goobstation.Common.Traits.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Goobstation.Shared.Traits.Systems;

public sealed partial class SocialAnxietySystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SocialAnxietyComponent, InteractionSuccessEvent>(OnHug);
    }
    private void OnHug(EntityUid uid, SocialAnxietyComponent component, ref InteractionSuccessEvent args)
    {
        _stun.TryKnockdown(uid, component.DownedTime);
        _stun.TryUpdateParalyzeDuration(uid, component.DownedTime);
        var mobName = Identity.Name(uid, EntityManager);
        _popupSystem.PopupEntity(Loc.GetString("social-anxiety-hugged", ("user", mobName)), uid, PopupType.MediumCaution);
    }
}
