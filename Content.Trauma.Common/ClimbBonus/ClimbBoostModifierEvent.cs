namespace Content.Trauma.Common.ClimbBonus;

[ByRefEvent]
public record struct ClimbBoostModifierEvent(float Coefficient, bool Handled);
