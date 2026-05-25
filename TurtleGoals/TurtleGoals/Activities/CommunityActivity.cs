using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Firebase.Auth;
using TurtleGoals.Adapters;
using TurtleGoals.Helpers;
using TurtleGoals.Models;

namespace TurtleGoals.Activities
{
    [Activity(Label = "Community", Theme = "@style/AppTheme")]
    public class CommunityActivity : AppCompatActivity
    {
        private RecyclerView _recyclerView;
        private EditText _etSearch;
        private CommunityFeedAdapter _adapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Guard: if no user is signed in, redirect to auth
            if (FirebaseAuth.Instance.CurrentUser == null)
            {
                var authIntent = new Intent(this, typeof(AuthActivity));
                authIntent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
                StartActivity(authIntent);
                Finish();
                return;
            }

            SetContentView(Resource.Layout.community_layout);

            _recyclerView = FindViewById<RecyclerView>(Resource.Id.rv_community_feed);
            _etSearch     = FindViewById<EditText>(Resource.Id.et_search);

            // Set up RecyclerView with an empty adapter while data loads
            _adapter = new CommunityFeedAdapter(new List<GoalModel>());
            _recyclerView.SetLayoutManager(new LinearLayoutManager(this));
            _recyclerView.SetAdapter(_adapter);

            // Live search filter
            _etSearch.TextChanged += (s, e) => _adapter.Filter(_etSearch.Text);

            // Load public goals from Firestore
            LoadPublicGoals();
        }

        private async void LoadPublicGoals()
        {
            try
            {
                var goals = await FirestoreService.Instance.GetPublicGoals();
                _adapter.UpdateData(goals);
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Could not load goals: " + ex.Message, ToastLength.Long).Show();
            }
        }
    }
}
