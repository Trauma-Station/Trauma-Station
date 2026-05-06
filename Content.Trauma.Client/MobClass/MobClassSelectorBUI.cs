using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.MobClass;

[UsedImplicitly]
public sealed class MobClassSelectorBui : BoundUserInterface
{
    private MobClassSelectorWindow? _window;

    public MobClassSelectorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        base.Open();

        _window = this.CreateWindow<MobClassSelectorWindow>();
    }
}
