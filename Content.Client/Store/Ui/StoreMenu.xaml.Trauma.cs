using Robust.Client.UserInterface.Controls;

namespace Content.Client.Store.Ui;

public sealed partial class StoreMenu
{
    public bool JobListingsSelected = false;

    public void SetJobListingsButtonVisibility(bool visible)
    {
        JobListingsButton.Visible = visible;
    }

    private void OnJobListingsButtonPressed(BaseButton.ButtonEventArgs args)
    {
        JobListingsSelected = true;
        UpdateListing();

        foreach (var child in CategoryListContainer.Children)
        {
            if (child is StoreCategoryButton button)
                button.Pressed = false;
        }
    }

    public void UnpressJobListingsButton()
    {
        JobListingsSelected = false;
        JobListingsButton.Pressed = false;
    }
}
