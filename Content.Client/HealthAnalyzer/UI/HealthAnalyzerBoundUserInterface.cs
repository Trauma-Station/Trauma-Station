// <Trauma>
using Content.Shared._Shitmed.Medical.HealthAnalyzer;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
// </Trauma>
using Content.Shared.MedicalScanner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.HealthAnalyzer.UI
{
    [UsedImplicitly]
    public sealed class HealthAnalyzerBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private HealthAnalyzerWindow? _window;

        public HealthAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<HealthAnalyzerWindow>();
            // <Shitmed>
            _window.OnBodyPartSelected += SendBodyPartMessage;
            _window.OnModeChanged += SendModeMessage;
            // </Shitmed>
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            if (_window == null)
                return;

            if (message is not HealthAnalyzerScannedUserMessage cast)
                return;

            _window.Populate(cast);
        }

        // <Shitmed>
        // TODO SHITMED: just use target stored on the component holy goida
        private void SendBodyPartMessage(ProtoId<OrganCategoryPrototype>? part, EntityUid target) => SendMessage(new HealthAnalyzerPartMessage(EntMan.GetNetEntity(target), part));

        private void SendModeMessage(HealthAnalyzerMode mode, EntityUid target) => SendMessage(new HealthAnalyzerModeSelectedMessage(EntMan.GetNetEntity(target), mode));
        // </Shitmed>
    }
}
