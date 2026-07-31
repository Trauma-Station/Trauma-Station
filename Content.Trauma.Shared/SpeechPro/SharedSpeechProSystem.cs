// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.UserInterface;

namespace Content.Trauma.Shared.SpeechPro;

public sealed partial class SharedSpeechProSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechProComponent, UseInHandEvent>(OnUseInHand, before: [typeof(ActivatableUISystem)]);
        SubscribeLocalEvent<SpeechProComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
        SubscribeLocalEvent<SpeechProComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnUseInHand(Entity<SpeechProComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !TryComp(ent.Owner, out ItemToggleComponent? toggle) || toggle.Activated)
            return;

        args.Handled = _itemToggle.TryActivate((ent.Owner, toggle), args.User, predicted: toggle.Predictable);
    }

    private void OnUiOpenAttempt(Entity<SpeechProComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (_itemToggle.IsActivated(ent.Owner))
            return;

        args.Cancel();
    }

    private void OnToggled(Entity<SpeechProComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && TryComp(ent.Owner, out ActivatableUIComponent? ui) && ui.Key != null)
            _ui.CloseUi(ent.Owner, ui.Key);
    }
}
