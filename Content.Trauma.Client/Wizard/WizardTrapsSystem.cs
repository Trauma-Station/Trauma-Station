// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Sprite;
using Content.Trauma.Shared.Wizard.Traps;

namespace Content.Trauma.Client.Wizard;

public sealed partial class WizardTrapsSystem : SharedWizardTrapsSystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private CommonSpriteVisibilitySystem _spriteVis = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WizardTrapComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<WizardTrapComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData(ent, TrapVisuals.Alpha, out float alpha, args.Component))
            return;

        _spriteVis.UpdateVisibilityModifiers(ent, nameof(WizardTrapComponent), alpha);
    }
}
