namespace Content.Trauma.Common.EntityEffects;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class EntityEffectNetworkEvent : EntityEventArgs
{
    [DataField]
    public NetEntity Entity;
}
