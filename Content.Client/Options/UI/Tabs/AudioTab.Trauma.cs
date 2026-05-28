namespace Content.Client.Options.UI.Tabs;

public sealed partial class AudioTab
{
    private void UpdateAcousticButtons(bool value)
    {
        AcousticHighResolutionCheckBox.Visible = value;
        SliderAcousticReflectionCount.Visible = value;
    }
}
