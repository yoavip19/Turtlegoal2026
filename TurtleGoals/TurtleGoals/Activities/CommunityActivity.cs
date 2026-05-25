using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
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
        private Android.OS.Handler _searchHandler;
        private Java.Lang.Runnable _searchRunnable;

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

            // Set up toolbar with back navigation
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar_community);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Android.Resource.Drawable.IcActionBack);
            toolbar.NavigationClick += (s, e) => Finish();

            _recyclerView = FindViewById<RecyclerView>(Resource.Id.rv_community_feed);
            _etSearch     = FindViewById<EditText>(Resource.Id.et_search);

            // Set up RecyclerView with an empty adapter while data loads
            _adapter = new CommunityFeedAdapter(new List<GoalModel>());
            _recyclerView.SetLayoutManager(new LinearLayoutManager(this));
            _recyclerView.SetAdapter(_adapter);

            // Debounced search — only filters 300 ms after the user stops typing
            _searchHandler = new Android.OS.Handler(Android.OS.Looper.MainLooper);
            _etSearch.TextChanged += (s, e) =>
            {
                _searchHandler.RemoveCallbacks(_searchRunnable);
                _searchRunnable = new Java.Lang.Runnable(() => _adapter.Filter(_etSearch.Text));
                _searchHandler.PostDelayed(_searchRunnable, 300);
            };

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

