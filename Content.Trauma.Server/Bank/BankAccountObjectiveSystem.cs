using System.Linq;
using Content.Shared.Objectives.Components;
using Content.Trauma.Shared.Bank;

namespace Content.Trauma.Server.Bank;

public sealed partial class BankAccountObjectiveSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BankAccountObjectiveComponent, ObjectiveGetProgressEvent>(OnProgress);
    }

    private void OnProgress(Entity<BankAccountObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0.0f;
        string data = "";
        foreach (var (bank, account, password) in ent.Comp.Details.ToList())
        {
            if (!Exists(bank))
            {
                ent.Comp.Details.Remove((bank, account, password));
                continue;
            }
            data += $"Bank: {bank.Comp.BankId}\n | Account: {account} | Password: {password}";
        }
        _meta.SetEntityDescription(ent, data);
    }
}
