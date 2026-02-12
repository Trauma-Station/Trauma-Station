namespace Content.Medical.Shared.Surgery;

[ByRefEvent]
public record struct SurgerySanitizationEvent(bool Handled = false);
