using Android.App;
using Android.Content.PM;
using Android.OS;
using WashTrack.MVVM.Views;

namespace WashTrack
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // Shell's top-level pages (Dashboard, Inventory, etc.) are flat
        // ShellContent roots reached via absolute ("///") navigation, so
        // they carry no push stack. With nothing to pop, the OS default is
        // to finish the activity — i.e. back silently exits the app from
        // any main page. Intercept that case: bounce to Dashboard first,
        // and only exit from Dashboard itself after confirmation.
        public override void OnBackPressed()
        {
            var shell = Shell.Current;
            if (shell != null && shell.Navigation.NavigationStack.Count == 0)
            {
                HandleRootBackPress(shell);
                return;
            }

            base.OnBackPressed();
        }

        private async void HandleRootBackPress(Shell shell)
        {
            if (shell.CurrentPage is not DashboardPage)
            {
                await shell.GoToAsync("///DashboardPage");
                return;
            }

            bool exit = await shell.DisplayAlert("Exit WashTrack", "Are you sure you want to exit?", "Exit", "Cancel");
            if (exit)
                FinishAffinity();
        }
    }
}
