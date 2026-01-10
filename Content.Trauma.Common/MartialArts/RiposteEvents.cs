namespace Content.Trauma.Common.MartialArts;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseRiposteCheckEvent : HandledEntityEventArgs;

public sealed partial class CanDoCQCEvent : BaseRiposteCheckEvent;
