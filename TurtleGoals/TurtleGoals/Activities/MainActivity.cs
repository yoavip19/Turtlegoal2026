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
        private MaterialButton btnLogout;

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
            btnLogout         = FindViewById<MaterialButton>(Resource.Id.btnLogout);

            // Set today's date
            tvDate.Text = DateTime.Now.ToString("dddd, MMMM d");

            // Button handlers
            btnCreateGoal.Click += (s, e) => ShowCreateGoalDialog();
            btnLogout.Click     += (s, e) => Logout();

            // TODO: CommunityActivity is planned (community_layout.xml is ready) but not yet implemented.
            // Once CommunityActivity is created, un-comment both the button in activity_dashboard.xml
            // and the lines below:
            // var btnCommunity = FindViewById<MaterialButton>(Resource.Id.btnCommunity);
            // btnCommunity.Click += (s, e) => StartActivity(new Intent(this, typeof(CommunityActivity)));

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

        // ── Create Goal dialog ────────────────────────────────────────────

        private void ShowCreateGoalDialog()
        {
            int dp16 = (int)(16 * Resources.DisplayMetrics.Density);

            var container = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical
            };
            container.SetPadding(dp16, dp16, dp16, 0);

            var etTitle = new EditText(this)
            {
                Hint = "Goal title",
                InputType = Android.Text.InputTypes.TextFlagCapSentences
            };
            var etDesc = new EditText(this)
            {
                Hint = "Description (optional)",
                InputType = Android.Text.InputTypes.TextFlagCapSentences | Android.Text.InputTypes.TextFlagMultiLine
            };

            container.AddView(etTitle);
            container.AddView(etDesc);

            new AlertDialog.Builder(this)
                .SetTitle("New Goal")
                .SetView(container)
                .SetPositiveButton("Create", async (s, e) =>
                {
                    string title = etTitle.Text?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(title))
                    {
                        Toast.MakeText(this, "Please enter a goal title", ToastLength.Short).Show();
                        return;
                    }

                    btnCreateGoal.Enabled = false;
                    try
                    {
                        var goal = new GoalModel
                        {
                            UserId      = _userId,
                            Title       = title,
                            Description = etDesc.Text?.Trim() ?? string.Empty,
                            IsPublic    = true  // All goals are public by default; a privacy toggle can be added later
                        };
                        await FirestoreService.Instance.CreateGoal(goal);
                        await LoadDashboardDataAsync();
                    }
                    catch (Exception ex)
                    {
                        RunOnUiThread(() =>
                            Toast.MakeText(this, "Could not create goal: " + ex.Message, ToastLength.Long).Show());
                    }
                    finally
                    {
                        RunOnUiThread(() => btnCreateGoal.Enabled = true);
                    }
                })
                .SetNegativeButton("Cancel", (s, e) => { })
                .Show();
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