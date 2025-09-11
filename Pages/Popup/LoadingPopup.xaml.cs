using Mopups.Pages;

namespace fbapp.Pages.Popup;

public partial class LoadingPopup : PopupPage
{
	public LoadingPopup()
	{
		InitializeComponent();

        BackgroundColor = Color.FromRgba(50, 50, 50, 128);
    }
}