using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Firebase.Auth;
using TurtleGoals.Adapters;
using TurtleGoals.Helpers;
using TurtleGoals.Models;

namespace TurtleGoals.Activities
{
    /// <summary>
    /// Goal Roadmap page showing a goal's tasks with progress.
    /// 
    /// Two use cases:
    ///   1. User (goal owner): can complete tasks and watch tips from the community.
    ///   2. Tipper (community viewer): can watch tasks and give tips on individual tasks.
    /// </summary>
    [Activity(Label = "Goal Roadmap", Theme = "@style/AppTheme.NoActionBar")]
    public class GoalRoadmapActivity : AppCompatActivity
    {
        private string _goalId;
        private GoalModel _goal;
        private bool _isOwner;

        // UI elements
        private TextView _tvGoalTitle;
        private TextView _tvGoalDescription;
        private ProgressBar _pbProgress;
        private TextView _tvProgressLabel;
        private RecyclerView _rvTasks;
        private LinearLayout _llTipInput;
        private EditText _etTipText;
        private Button _btnSendTip;

        private RoadmapTaskAdapter _adapter;
        private List<TaskModel> _tasks = new List<TaskModel>();
        private Dictionary<string, List<CommentModel>> _tipsByTask = new Dictionary<string, List<CommentModel>>();
        private string _selectedTaskIdForTip;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_goal_roadmap);

            // Get goalId from intent
            _goalId = Intent.GetStringExtra("goalId");
            if (string.IsNullOrEmpty(_goalId))
            {
                Toast.MakeText(this, "No goal specified.", ToastLength.Short).Show();
                Finish();
                return;
            }

            // Set up toolbar with back navigation
            var toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar_roadmap);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Goal Roadmap";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            toolbar.NavigationClick += (s, e) => Finish();

            // Bind UI
            _tvGoalTitle = FindViewById<TextView>(Resource.Id.tv_roadmap_goal_title);
            _tvGoalDescription = FindViewById<TextView>(Resource.Id.tv_roadmap_goal_description);
            _pbProgress = FindViewById<ProgressBar>(Resource.Id.pb_roadmap_progress);
            _tvProgressLabel = FindViewById<TextView>(Resource.Id.tv_roadmap_progress_label);
            _rvTasks = FindViewById<RecyclerView>(Resource.Id.rv_roadmap_tasks);
            _llTipInput = FindViewById<LinearLayout>(Resource.Id.ll_tip_input);
            _etTipText = FindViewById<EditText>(Resource.Id.et_tip_text);
            _btnSendTip = FindViewById<Button>(Resource.Id.btn_send_tip);

            _rvTasks.SetLayoutManager(new LinearLayoutManager(this));

            // Send tip button
            _btnSendTip.Click += OnSendTipClicked;

            // Load goal data
            LoadGoalData();
        }

        private async void LoadGoalData()
        {
            try
            {
                // Load tasks
                _tasks = await FirestoreService.Instance.GetGoalTasks(_goalId);

                // Determine if current user is the goal owner
                var currentUserId = FirebaseAuth.Instance.CurrentUser?.Uid;

                // We need to get goal metadata to check ownership
                var goals = await FirestoreService.Instance.GetUserGoals(currentUserId);
                _goal = goals.FirstOrDefault(g => g.GoalId == _goalId);

                if (_goal != null)
                {
                    _isOwner = true;
                }
                else
                {
                    // Not the owner — load from public goals
                    var publicGoals = await FirestoreService.Instance.GetPublicGoals();
                    _goal = publicGoals.FirstOrDefault(g => g.GoalId == _goalId);
                    _isOwner = false;
                }

                if (_goal == null)
                {
                    Toast.MakeText(this, "Goal not found.", ToastLength.Short).Show();
                    Finish();
                    return;
                }

                // Load tips for all tasks
                await LoadTipsForTasks();

                // Update UI
                RunOnUiThread(() => BindGoalData());
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Error loading roadmap: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private async System.Threading.Tasks.Task LoadTipsForTasks()
        {
            _tipsByTask.Clear();
            foreach (var task in _tasks)
            {
                if (!string.IsNullOrEmpty(task.TaskId))
                {
                    var comments = await FirestoreService.Instance.GetTaskComments(task.TaskId);
                    if (comments.Count > 0)
                        _tipsByTask[task.TaskId] = comments;
                }
            }
        }

        private void BindGoalData()
        {
            // Header
            _tvGoalTitle.Text = _goal.Title ?? "Untitled Goal";
            _tvGoalDescription.Text = _goal.Description ?? "";
            _tvGoalDescription.Visibility = string.IsNullOrWhiteSpace(_goal.Description)
                ? ViewStates.Gone
                : ViewStates.Visible;

            // Progress
            UpdateProgress();

            // Tip input area: visible only for tippers
            _llTipInput.Visibility = _isOwner ? ViewStates.Gone : ViewStates.Visible;

            // Set up adapter
            _adapter = new RoadmapTaskAdapter(_tasks, _isOwner, _tipsByTask);
            _rvTasks.SetAdapter(_adapter);

            if (_isOwner)
            {
                // User can toggle tasks
                _adapter.TaskToggled += OnTaskToggled;
            }
            else
            {
                // Tipper can request to give a tip
                _adapter.TipRequested += OnTipRequested;
            }
        }

        private void UpdateProgress()
        {
            int total = _tasks.Count;
            int done = _tasks.Count(t => t.IsDone);
            int percent = total > 0 ? (done * 100) / total : 0;

            _pbProgress.Progress = percent;
            _tvProgressLabel.Text = $"{done}/{total} tasks completed ({percent}%)";
        }

        private async void OnTaskToggled(TaskModel task, bool isDone)
        {
            try
            {
                bool success = await FirestoreService.Instance.SetTaskDone(_goalId, task.TaskId, isDone);
                if (success)
                {
                    task.IsDone = isDone;
                    task.CompletedAt = isDone ? DateTime.UtcNow : (DateTime?)null;
                    RunOnUiThread(() =>
                    {
                        UpdateProgress();
                        _adapter.NotifyDataSetChanged();
                    });
                }
                else
                {
                    RunOnUiThread(() =>
                        Toast.MakeText(this, "Failed to update task.", ToastLength.Short).Show());
                }
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                    Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Short).Show());
            }
        }

        private void OnTipRequested(TaskModel task)
        {
            _selectedTaskIdForTip = task.TaskId;
            _etTipText.RequestFocus();
            _etTipText.Hint = $"Write a tip for \"{task.Title}\"…";
        }

        private async void OnSendTipClicked(object sender, EventArgs e)
        {
            var tipText = _etTipText.Text?.Trim();
            if (string.IsNullOrEmpty(tipText))
            {
                Toast.MakeText(this, "Please enter a tip.", ToastLength.Short).Show();
                return;
            }

            if (string.IsNullOrEmpty(_selectedTaskIdForTip))
            {
                Toast.MakeText(this, "Please tap a task's Tip button first.", ToastLength.Short).Show();
                return;
            }

            try
            {
                var currentUser = FirebaseAuth.Instance.CurrentUser;
                var comment = new CommentModel
                {
                    GoalId = _goalId,
                    TaskId = _selectedTaskIdForTip,
                    UserId = currentUser?.Uid ?? "",
                    UserName = currentUser?.DisplayName ?? currentUser?.Email ?? "Anonymous",
                    Text = tipText
                };

                bool success = await FirestoreService.Instance.CreateComment(comment);
                if (success)
                {
                    _etTipText.Text = "";
                    _etTipText.Hint = "Write a tip…";

                    // Reload tips and refresh
                    await LoadTipsForTasks();
                    RunOnUiThread(() => _adapter.UpdateData(_tasks, _tipsByTask));

                    Toast.MakeText(this, "Tip sent! 💡", ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(this, "Failed to send tip.", ToastLength.Short).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Short).Show();
            }
        }
    }
}
