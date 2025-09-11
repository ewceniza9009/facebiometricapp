using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbapp.Pages.Popup
{
    public static class MaskedInputPromptHelper
    {
        public static async Task<string> DisplayMaskedPromptAsync(
            string title,
            string message,
            string initialValue = "",
            int maxLength = 100,
            string placeholder = "")
        {
            TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();
            var passwordEntry = new Entry
            {
                IsPassword = true,
                Text = initialValue,
                MaxLength = maxLength,
                Placeholder = placeholder,
            };

            var submitButton = new Button { Text = "OK", TextColor = Colors.White, BackgroundColor = Color.FromHex("#1ea883"), CornerRadius = 8 };
            var cancelButton = new Button { Text = "Cancel", TextColor = Colors.White, BackgroundColor = Colors.Gray, CornerRadius = 8 };

            submitButton.Clicked += (s, e) =>
            {
                taskCompletionSource.TrySetResult(passwordEntry.Text);
                Application.Current.MainPage.Navigation.PopModalAsync();
            };

            cancelButton.Clicked += (s, e) =>
            {
                taskCompletionSource.TrySetResult(null); // Return null if canceled
                Application.Current.MainPage.Navigation.PopModalAsync();
            };

            var layout = new StackLayout
            {
                Padding = new Thickness(20),
                Children =
                {
                    new Label { Text = title, FontSize = 20, FontAttributes = FontAttributes.Bold },
                    new Label { Text = message, FontSize = 14 },
                    passwordEntry,
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.EndAndExpand,
                        Children = { cancelButton, submitButton },
                        Spacing = 10,
                        Padding = new Thickness(0, 10, 0, 0)
                    }
                }
            };

            var promptPage = new ContentPage
            {
                Content = new Frame
                {
                    BorderColor = Colors.Gray,
                    CornerRadius = 10,
                    BackgroundColor = Colors.White,
                    Content = layout,
                    VerticalOptions = LayoutOptions.CenterAndExpand,
                    HorizontalOptions = LayoutOptions.CenterAndExpand
                },
                BackgroundColor = new Color(0, 0, 0, 0.5f) // Semi-transparent background
            };

            await Application.Current.MainPage.Navigation.PushModalAsync(promptPage);

            return await taskCompletionSource.Task; // Wait for user input or cancel
        }
    }
}
