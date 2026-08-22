using Content.Trauma.Shared.Syndicate.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Syndicate.UI
{
    [UsedImplicitly]
    public sealed class SyndicateConverterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private SyndicateConverterMenu? _menu;

        public SyndicateConverterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<SyndicateConverterMenu>();
            _menu.SetEntity(Owner);

            _menu.ConvertButtonPressed += () =>
            {
                SendMessage(new SyndicateConverterStartPackBuiMessage());
            };

            _menu.OpenCentered();
        }
    }
}
