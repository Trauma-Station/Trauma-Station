// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle;
using Content.Shared.UserInterface;

namespace Content.Trauma.Shared.SpeechPro;

public sealed partial class SharedSpeechProSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechProComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
    }

    private void OnUiOpenAttempt(Entity<SpeechProComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (_itemToggle.IsActivated(ent.Owner))
            return;

        args.Cancel();
    }
}
