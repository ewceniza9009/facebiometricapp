using Mopups.Pages;
using Mopups.Services;

public class ImagePopup : PopupPage
{
    public ImagePopup(ImageSource imageSource)
    {
        BackgroundColor = Color.FromRgba(50, 50, 50, 128);

        Content = new Frame
        {            
            Margin = 20,            
            HeightRequest = 400,
            WidthRequest = 290,
            Content = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center, // Center the layout horizontally
                VerticalOptions = LayoutOptions.Center, // Center the layout vertically
                Children =
                {
                    new Image
                    {
                        Source = imageSource,
                        HeightRequest = 300,
                        WidthRequest = 280,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Button
                    {
                        Text = "Close",
                        Margin = 5,
                        Command = new Command(() => MopupService.Instance.PopAsync())
                    }
                }
            }
        };
    }
}