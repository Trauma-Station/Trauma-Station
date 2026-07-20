// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Shared.Augments;

public sealed partial class AugmentActivatableUISystem : EntitySystem
{
    [Dependency] private AugmentSystem _augment = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AugmentActivatableUIComponent, AugmentActionEvent>(OnAugmentAction);
    }

    private void OnAugmentAction(Entity<AugmentActivatableUIComponent> augment, ref AugmentActionEvent args)
    {
        if (_augment.GetBody(augment) is not {} body ||
            augment.Comp.Key is not {} key ||
            !_ui.HasUi(augment, key))
            return;

        _ui.OpenUi(augment.Owner, augment.Comp.Key, body);
        args.Handled = true;
    }
}
