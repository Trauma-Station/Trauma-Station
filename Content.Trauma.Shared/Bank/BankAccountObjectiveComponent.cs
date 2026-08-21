namespace Content.Trauma.Shared.Bank;

[RegisterComponent]
public sealed partial class BankAccountObjectiveComponent : Component
{
    [DataField]
    public List<(Entity<BankComponent>, string, string)> Details = new();
}
