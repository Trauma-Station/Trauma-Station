// <Trauma>
using Content.Shared._Shitmed.Medical.HealthAnalyzer;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
// </Trauma>
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Cryogenics;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
namespace Content.Client.Medical.Cryogenics;

[UsedImplicitly]
public sealed class CryoPodBoundUserInterface : BoundUserInterface
{
    private CryoPodWindow? _window;

    public CryoPodBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindowCenteredLeft<CryoPodWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OnEjectPatientPressed += EjectPatientPressed;
        _window.OnEjectBeakerPressed += EjectBeakerPressed;
        _window.OnInjectPressed += InjectPressed;
        // <Shitmed>
        _window.OnBodyPartSelected += SendBodyPartMessage;
        _window.OnModeChanged += SendModeMessage;
        // </Shitmed>
    }

    private void EjectPatientPressed()
    {
        var isLocked =
            EntMan.TryGetComponent<CryoPodComponent>(Owner, out var cryoComp)
            && cryoComp.Locked;

        _window?.SetEjectErrorVisible(isLocked);
        SendMessage(new CryoPodSimpleUiMessage(CryoPodSimpleUiMessage.MessageType.EjectPatient));
    }

    private void EjectBeakerPressed()
    {
        SendMessage(new CryoPodSimpleUiMessage(CryoPodSimpleUiMessage.MessageType.EjectBeaker));
    }

    private void InjectPressed(FixedPoint2 transferAmount)
    {
        SendMessage(new CryoPodInjectUiMessage(transferAmount));
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window != null && message is CryoPodUserMessage cryoMsg)
        {
            _window.Populate(cryoMsg);
        }
    }

    // <Shitmed>
    // TODO SHITMED: just use target stored on the component holy goida
    private void SendBodyPartMessage(ProtoId<OrganCategoryPrototype>? part, EntityUid target) => SendMessage(new HealthAnalyzerPartMessage(EntMan.GetNetEntity(target), part));

    private void SendModeMessage(HealthAnalyzerMode mode, EntityUid target) => SendMessage(new HealthAnalyzerModeSelectedMessage(EntMan.GetNetEntity(target), mode));
    // </Shitmed>
}
