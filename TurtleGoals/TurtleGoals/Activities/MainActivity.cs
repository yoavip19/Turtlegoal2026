using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Firebase.Auth;
using Google.Android.Material.Button;
using TurtleGoals.Helpers;
using TurtleGoals.Models;

namespace TurtleGoals.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme")]
    public class MainActivity : AppCompatActivity
    {
        // ── Views ─────────────────────────────────────────────────────────
        private TextView tvGreeting;
        private TextView tvDate;
        private TextView tvGoalsCount;
        private TextView tvCompletedTasks;
        private TextView tvProgressPercent;
        private LinearLayout llTasksContainer;
        private TextView tvNoTasks;
        private TextView tvTasksLoading;
        private MaterialButton btnCreateGoal;
        private MaterialButton btnCommunity;
        private MaterialButton btnLogout;
        private RecyclerView rvGoalsBanner;
        private TextView tvNoGoals;
        private GoalBannerAdapter _goalBannerAdapter;

        // ── State ─────────────────────────────────────────────────────────
        private string _userId;
        private List<GoalModel> _goals = new List<GoalModel>();
        private Dictionary<string, List<TaskModel>> _goalTasks = new Dictionary<string, List<TaskModel>>();

        // ─────────────────────────────────────────────────────────────────
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            // Guard: if no user is signed in, return to auth screen
            if (FirebaseAuth.Instance.CurrentUser == null)
            {
                NavigateToAuth();
                return;
            }

            _userId = FirebaseAuth.Instance.CurrentUser.Uid;

            SetContentView(Resource.Layout.activity_dashboard);

            // Wire views
            tvGreeting        = FindViewById<TextView>(Resource.Id.tvGreeting);
            tvDate            = FindViewById<TextView>(Resource.Id.tvDate);
            tvGoalsCount      = FindViewById<TextView>(Resource.Id.tvGoalsCount);
            tvCompletedTasks  = FindViewById<TextView>(Resource.Id.tvCompletedTasks);
            tvProgressPercent = FindViewById<TextView>(Resource.Id.tvProgressPercent);
            llTasksContainer  = FindViewById<LinearLayout>(Resource.Id.llTasksContainer);
            tvNoTasks         = FindViewById<TextView>(Resource.Id.tvNoTasks);
            tvTasksLoading    = FindViewById<TextView>(Resource.Id.tvTasksLoading);
            btnCreateGoal     = FindViewById<MaterialButton>(Resource.Id.btnCreateGoal);
            btnCommunity      = FindViewById<MaterialButton>(Resource.Id.btnCommunity);
            btnLogout         = FindViewById<MaterialButton>(Resource.Id.btnLogout);
            rvGoalsBanner     = FindViewById<RecyclerView>(Resource.Id.rvGoalsBanner);
            tvNoGoals         = FindViewById<TextView>(Resource.Id.tvNoGoals);

            // Set up the horizontal goals RecyclerView
            _goalBannerAdapter = new GoalBannerAdapter(this, _goals);
            rvGoalsBanner.SetLayoutManager(new LinearLayoutManager(this, LinearLayoutManager.Horizontal, false));
            rvGoalsBanner.SetAdapter(_goalBannerAdapter);

            // Set today's date
            tvDate.Text = DateTime.Now.ToString("dddd, MMMM d");

            // Button handlers
            btnCreateGoal.Click += (s, e) => StartActivityForResult(new Intent(this, typeof(CreateGoalActivity)), 1001);
            btnCommunity.Click  += (s, e) => StartActivity(new Intent(this, typeof(CommunityActivity)));
            btnLogout.Click     += (s, e) => Logout();

            // Load data from Firebase
            _ = LoadDashboardDataAsync();
        }

        // ── Data loading ──────────────────────────────────────────────────

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // Load user profile for the greeting
                var user = await FirestoreService.Instance.GetUser(_userId);
                string name = user?.Name ?? FirebaseAuth.Instance.CurrentUser?.DisplayName;
                RunOnUiThread(() =>
                    tvGreeting.Text = string.IsNullOrWhiteSpace(name)
                        ? "Hey there! 👋"
                        : $"Hey, {name}! 👋");

                // Load goals
                _goals = await FirestoreService.Instance.GetUserGoals(_userId);

                // Load tasks for every goal
                _goalTasks.Clear();
                foreach (var goal in _goals)
                {
                    var tasks = await FirestoreService.Instance.GetGoalTasks(goal.GoalId);
                    _goalTasks[goal.GoalId] = tasks;
                }

                RunOnUiThread(UpdateDashboardUI);
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                    Toast.MakeText(this, "Could not load data: " + ex.Message, ToastLength.Long).Show());
            }
        }

        // ── UI update ─────────────────────────────────────────────────────

        private void UpdateDashboardUI()
        {
            // Goals banner
            _goalBannerAdapter.UpdateGoals(_goals);
            if (_goals.Count > 0)
            {
                rvGoalsBanner.Visibility = ViewStates.Visible;
                tvNoGoals.Visibility     = ViewStates.Gone;
            }
            else
            {
                rvGoalsBanner.Visibility = ViewStates.Gone;
                tvNoGoals.Visibility     = ViewStates.Visible;
            }

            // Progress stats
            int totalTasks     = _goalTasks.Values.Sum(t => t.Count);
            int completedCount = _goalTasks.Values.Sum(t => t.Count(task => task.IsDone));
            int percent        = totalTasks > 0 ? (int)(completedCount * 100.0 / totalTasks) : 0;

            tvGoalsCount.Text      = _goals.Count.ToString();
            tvCompletedTasks.Text  = completedCount.ToString();
            tvProgressPercent.Text = $"{percent}%";

            // Today's tasks — first incomplete task from each goal (ordered by task order)
            llTasksContainer.RemoveAllViews();
            tvTasksLoading.Visibility = ViewStates.Gone;

            bool hasPendingTask = false;
            foreach (var goal in _goals)
            {
                if (!_goalTasks.TryGetValue(goal.GoalId, out var tasks)) continue;
                var nextTask = tasks.OrderBy(t => t.Order).FirstOrDefault(t => !t.IsDone);
                if (nextTask == null) continue;

                hasPendingTask = true;
                AddTaskItemView(goal, nextTask);
            }

            tvNoTasks.Visibility = hasPendingTask ? ViewStates.Gone : ViewStates.Visible;
        }

        private void AddTaskItemView(GoalModel goal, TaskModel task)
        {
            var itemView   = LayoutInflater.Inflate(Resource.Layout.task_item, llTasksContainer, false);
            var cbDone     = itemView.FindViewById<CheckBox>(Resource.Id.cbTaskDone);
            var tvTitle    = itemView.FindViewById<TextView>(Resource.Id.tvTaskTitle);
            var tvGoalName = itemView.FindViewById<TextView>(Resource.Id.tvTaskGoal);

            tvTitle.Text    = task.Title;
            tvGoalName.Text = goal.Title;
            cbDone.Checked  = task.IsDone;

            // Wire the checkbox — set after Checked assignment so it does not fire on init
            cbDone.CheckedChange += async (s, e) =>
            {
                cbDone.Enabled = false;
                try
                {
                    await FirestoreService.Instance.SetTaskDone(goal.GoalId, task.TaskId, e.IsChecked);
                    await LoadDashboardDataAsync();
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        cbDone.Enabled = true;
                        Toast.MakeText(this, "Could not update task: " + ex.Message, ToastLength.Short).Show();
                    });
                }
            };

            llTasksContainer.AddView(itemView);
        }

        // ── Activity result (reload data when returning from CreateGoalActivity) ──

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == 1001 && resultCode == Result.Ok)
            {
                _ = LoadDashboardDataAsync();
            }
        }

        // ── Logout ────────────────────────────────────────────────────────

        private void Logout()
        {
            FirebaseAuth.Instance.SignOut();
            NavigateToAuth();
        }

        private void NavigateToAuth()
        {
            var intent = new Intent(this, typeof(AuthActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
            StartActivity(intent);
            Finish();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}