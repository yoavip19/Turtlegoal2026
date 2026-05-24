using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Firebase.Auth;

namespace TurtleGoals.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme")]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            // Guard: if no user is signed in, send them back to the auth screen
            if (FirebaseAuth.Instance.CurrentUser == null)
            {
                var intent = new Intent(this, typeof(AuthActivity));
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
                StartActivity(intent);
                Finish();
                return;
            }

            SetContentView(Resource.Layout.activity_main);

            // TODO: CommunityActivity is planned (community_layout.xml is ready) but not yet implemented.
            // Once CommunityActivity is created, wire up the navigation button here:
            // var communityIntent = new Intent(this, typeof(CommunityActivity));
            // StartActivity(communityIntent);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}