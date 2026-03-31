namespace Content.Trauma.Common.Wizard;

public abstract partial class CommonWizardSystem : EntitySystem
{
    public abstract bool IsChunni(EntityUid? eyepatch);
    public abstract bool IsMovementBlocked(EntityUid? wizard);
}
