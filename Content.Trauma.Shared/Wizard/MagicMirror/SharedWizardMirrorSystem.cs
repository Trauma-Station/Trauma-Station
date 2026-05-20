// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Preferences;
using Content.Shared.UserInterface;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Wizard.MagicMirror;

public abstract partial class SharedWizardMirrorSystem : EntitySystem
{
    [Dependency] protected HumanoidProfileSystem Humanoid = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] protected SharedUserInterfaceSystem UISystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WizardMirrorComponent, AfterInteractEvent>(OnMagicMirrorInteract);
        SubscribeLocalEvent<WizardMirrorComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<WizardMirrorComponent, ActivatableUIOpenAttemptEvent>(OnAttemptOpenUI);
        SubscribeLocalEvent<WizardMirrorComponent, BoundUserInterfaceCheckRangeEvent>(OnMirrorRangeCheck);
    }

    private void OnMagicMirrorInteract(Entity<WizardMirrorComponent> mirror, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        UpdateInterface(mirror, args.Target.Value, mirror);
        UISystem.TryOpenUi(mirror.Owner, WizardMirrorUiKey.Key, args.User);
    }

    private void OnMirrorRangeCheck(EntityUid uid,
        WizardMirrorComponent component,
        ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (args.Result == BoundUserInterfaceRangeResult.Fail)
            return;

        if (component.Target == null || !Exists(component.Target))
        {
            component.Target = null;
            args.Result = BoundUserInterfaceRangeResult.Fail;
            return;
        }

        if (!_interaction.InRangeUnobstructed(component.Target.Value, uid))
            args.Result = BoundUserInterfaceRangeResult.Fail;
    }

    private void OnAttemptOpenUI(EntityUid uid, WizardMirrorComponent component, ref ActivatableUIOpenAttemptEvent args)
    {
        var user = component.Target ?? args.User;

        if (!HasComp<HumanoidProfileComponent>(user))
            args.Cancel();
    }

    private void OnBeforeUIOpen(Entity<WizardMirrorComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateInterface(ent, args.User, ent);
    }

    protected void UpdateInterface(EntityUid mirrorUid, EntityUid targetUid, WizardMirrorComponent component)
    {
        if (Humanoid.CreateProfile(targetUid) is not {} profile)
            return;

        component.Target = targetUid;
        Dirty(mirrorUid, component);

        var state = new WizardMirrorUiState(profile);
        UISystem.SetUiState(mirrorUid, WizardMirrorUiKey.Key, state);
    }
}

[Serializable, NetSerializable]
public enum WizardMirrorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class WizardMirrorUiState(HumanoidCharacterProfile profile) : BoundUserInterfaceState
{
    public HumanoidCharacterProfile Profile = profile;
}

[Serializable, NetSerializable]
public sealed class WizardMirrorMessage(HumanoidCharacterProfile profile) : BoundUserInterfaceMessage
{
    public HumanoidCharacterProfile Profile = profile;
}
