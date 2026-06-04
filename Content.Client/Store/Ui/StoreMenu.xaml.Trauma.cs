namespace Content.Client.Store.Ui;

public sealed partial class StoreMenu
{
    public void SetJobListingsButtonVisibility(bool visible)
    {
        JobListingsButton.Visible = visible;
    }
}
