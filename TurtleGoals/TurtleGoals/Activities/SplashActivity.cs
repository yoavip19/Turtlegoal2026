using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.AppCompat.App;

namespace TurtleGoals.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : AppCompatActivity
    {
        private readonly Android.OS.Handler _handler = new Android.OS.Handler(Android.OS.Looper.MainLooper);
        private Java.Lang.Runnable _navigateRunnable;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
        }

        protected override void OnResume()
        {
            base.OnResume();
            _navigateRunnable = new Java.Lang.Runnable(() =>
                StartActivity(new Intent(this, typeof(AuthActivity))));
            _handler.PostDelayed(_navigateRunnable, 3000);
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (_navigateRunnable != null)
                _handler.RemoveCallbacks(_navigateRunnable);
        }
    }
}
