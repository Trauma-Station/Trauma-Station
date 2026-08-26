namespace Content.Trauma.Shared.AntiTamper;

[Flags]
public enum AntiTamperAlertType
{
    None = 0,
    All = ~None,
    OnDamaged = 1 << 1,
    OnDestroyed = 1 << 2
}
