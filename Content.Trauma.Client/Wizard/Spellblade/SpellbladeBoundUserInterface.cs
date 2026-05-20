// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Wizard.Spellblade;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Wizard.Spellblade;

[UsedImplicitly]
public sealed partial class SpellbladeBoundUserInterface : BoundUserInterface
{
    [Dependency] private IClyde _displayManager = default!;
    [Dependency] private IInputManager _inputManager = default!;

    private SpellbladeMenu? _menu;

    public SpellbladeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SpellbladeMenu>();
        _menu.SetEntity(Owner);
        _menu.SendSpellbladeSystemMessageAction += SendSpellbladeSystemMessage;

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    public void SendSpellbladeSystemMessage(ProtoId<SpellbladeEnchantmentPrototype> protoId)
    {
        SendPredictedMessage(new SpellbladeEnchantMessage(protoId));
    }
}
