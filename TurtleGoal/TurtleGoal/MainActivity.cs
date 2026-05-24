using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Firebase.Firestore;
using Java.Util;
using TurtleGoal.Helpers;
using TurtleGoal.Models;

namespace TurtleGoal
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            // Trigger the test immediately on launch
            RunDatabaseTest();
        }
        private void RunDatabaseTest()
        {
            // 1. Build your C# object normally
            GoalModel newGoal = new GoalModel
            {
                UserId = "user_123",
                Title = "Learn Xamarin Native",
                Timeframe = "20 Hours",
                IsPublic = true,
                ProgressPercentage = 0,
                Category = "Coding"
            };

            // 2. Convert it to a HashMap right as you pass it to Firestore
            FirebaseFirestore.Instance
                .Collection("goals")
                .Add(newGoal.ToHashMap())
                .AddOnCompleteListener(new FirestoreTestListener());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}