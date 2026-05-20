namespace Content.Trauma.Client.Knowledge;

[ByRefEvent]
public record struct GetAttributeModifierEvent(List<(string Label, string Value)> Modifiers);
