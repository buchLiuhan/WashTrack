using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Activity;
using WashTrack.MVVM.Views;

namespace WashTrack
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // Overriding the legacy OnBackPressed() isn't reliable on Android 13+
        // (API 33+) — back handling goes through AndroidX's
        // OnBackPressedDispatcher instead, which can bypass that override
        // entirely. Registering a callback here is the mechanism that
        // actually gets invoked.
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            OnBackPressedDispatcher.AddCallback(this, new BackPressHandler(this));
        }

        // Shell's top-level pages (Dashboard, Inventory, etc.) are flat
        // ShellContent roots reached via absolute ("///") navigation, so
        // they carry no push stack. With nothing to pop, the OS default is
        // to finish the activity — i.e. back silently exits the app from
        // any main page. Intercept that case: bounce to Dashboard first,
        // and only exit from Dashboard itself after confirmation.
        private sealed class BackPressHandler : OnBackPressedCallback
        {
            private readonly MainActivity _activity;

            public BackPressHandler(MainActivity activity) : base(true)
            {
                _activity = activity;
            }

            public override async void HandleOnBackPressed()
            {
                var shell = Shell.Current;
                if (shell == null) return;

                if (shell.Navigation.NavigationStack.Count > 0)
                {
                    await shell.GoToAsync("..");
                    return;
                }

                if (shell.CurrentPage is not DashboardPage)
                {
                    await shell.GoToAsync("///DashboardPage");
                    return;
                }

                bool exit = await shell.DisplayAlert("Exit WashTrack", "Are you sure you want to exit?", "Exit", "Cancel");
                if (exit)
                    _activity.FinishAffinity();
            }
        }
    }
}
