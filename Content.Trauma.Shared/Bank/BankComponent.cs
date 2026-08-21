using Content.Shared.FixedPoint;
using Content.Shared.Store;

namespace Content.Trauma.Shared.Bank;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BankComponent : Component
{
    [DataField, AutoNetworkedField]
    public string BankId = string.Empty;

    [DataField, AutoNetworkedField]
    public Dictionary<string, FixedPoint2> Accounts = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, string> Passwords = new();

    [DataField(required: true)]
    public ProtoId<CurrencyPrototype> Currency = "Spesos";
}
