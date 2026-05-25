using System;
using System.Collections.Generic;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using TurtleGoals.Models;

namespace TurtleGoals.Adapters
{
    /// <summary>
    /// Adapter for the task list in the Goal Roadmap page.
    /// Supports two modes:
    ///   - Owner (user): can check/uncheck tasks to mark them done.
    ///   - Tipper: can view tasks and tap "Tip" to give advice on a task.
    /// </summary>
    public class RoadmapTaskAdapter : RecyclerView.Adapter
    {
        private List<TaskModel> _tasks;
        private readonly bool _isOwner;
        private readonly Dictionary<string, List<CommentModel>> _tipsByTask;

        /// <summary>Raised when the owner toggles a task's done state.</summary>
        public event Action<TaskModel, bool> TaskToggled;

        /// <summary>Raised when a tipper taps the Tip button for a task.</summary>
        public event Action<TaskModel> TipRequested;

        public RoadmapTaskAdapter(List<TaskModel> tasks, bool isOwner,
            Dictionary<string, List<CommentModel>> tipsByTask = null)
        {
            _tasks = tasks ?? new List<TaskModel>();
            _isOwner = isOwner;
            _tipsByTask = tipsByTask ?? new Dictionary<string, List<CommentModel>>();
        }

        public override int ItemCount => _tasks.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context)
                                     .Inflate(Resource.Layout.roadmap_task_item, parent, false);
            return new RoadmapTaskViewHolder(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = (RoadmapTaskViewHolder)holder;
            var task = _tasks[position];

            vh.TvTitle.Text = task.Title ?? "Untitled Task";

            // Description
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                vh.TvDescription.Visibility = ViewStates.Visible;
                vh.TvDescription.Text = task.Description;
            }
            else
            {
                vh.TvDescription.Visibility = ViewStates.Gone;
            }

            // Duration
            if (task.DurationDays > 0)
            {
                vh.TvDuration.Visibility = ViewStates.Visible;
                vh.TvDuration.Text = $"⏱ {task.DurationDays} day{(task.DurationDays == 1 ? "" : "s")}";
            }
            else
            {
                vh.TvDuration.Visibility = ViewStates.Gone;
            }

            // Strikethrough for completed tasks
            if (task.IsDone)
            {
                vh.TvTitle.PaintFlags = vh.TvTitle.PaintFlags | PaintFlags.StrikeThruText;
                vh.TvTitle.SetTextColor(Color.ParseColor("#B0A09A"));
            }
            else
            {
                vh.TvTitle.PaintFlags = vh.TvTitle.PaintFlags & ~PaintFlags.StrikeThruText;
                vh.TvTitle.SetTextColor(Color.ParseColor("#4F4541"));
            }

            // Owner mode: show checkbox, hide tip button
            if (_isOwner)
            {
                vh.CbDone.Visibility = ViewStates.Visible;
                vh.BtnTip.Visibility = ViewStates.Gone;

                vh.CbDone.CheckedChange -= vh.OnCheckedChangeHandler;
                vh.CbDone.Checked = task.IsDone;
                vh.OnCheckedChangeHandler = (s, e) =>
                {
                    TaskToggled?.Invoke(task, e.IsChecked);
                };
                vh.CbDone.CheckedChange += vh.OnCheckedChangeHandler;
            }
            else
            {
                // Tipper mode: hide checkbox, show tip button
                vh.CbDone.Visibility = ViewStates.Gone;
                vh.BtnTip.Visibility = ViewStates.Visible;

                vh.BtnTip.Click -= vh.OnTipClickHandler;
                vh.OnTipClickHandler = (s, e) =>
                {
                    TipRequested?.Invoke(task);
                };
                vh.BtnTip.Click += vh.OnTipClickHandler;
            }

            // Show tips/comments for this task
            vh.LlTips.RemoveAllViews();
            if (_tipsByTask.TryGetValue(task.TaskId ?? "", out var tips) && tips.Count > 0)
            {
                vh.LlTips.Visibility = ViewStates.Visible;
                foreach (var tip in tips)
                {
                    var tipView = new TextView(vh.ItemView.Context)
                    {
                        Text = $"💡 {tip.UserName}: {tip.Text}",
                        TextSize = 12
                    };
                    tipView.SetTextColor(Color.ParseColor("#6D574F"));
                    tipView.SetPadding(0, 4, 0, 4);
                    vh.LlTips.AddView(tipView);
                }
            }
            else
            {
                vh.LlTips.Visibility = ViewStates.Gone;
            }
        }

        public void UpdateData(List<TaskModel> tasks,
            Dictionary<string, List<CommentModel>> tipsByTask = null)
        {
            _tasks = tasks ?? new List<TaskModel>();
            if (tipsByTask != null)
            {
                _tipsByTask.Clear();
                foreach (var kvp in tipsByTask)
                    _tipsByTask[kvp.Key] = kvp.Value;
            }
            NotifyDataSetChanged();
        }

        // ── ViewHolder ──────────────────────────────────────────────────────────

        public class RoadmapTaskViewHolder : RecyclerView.ViewHolder
        {
            public CheckBox CbDone       { get; }
            public TextView TvTitle      { get; }
            public TextView TvDescription { get; }
            public TextView TvDuration   { get; }
            public Button   BtnTip       { get; }
            public LinearLayout LlTips   { get; }

            public EventHandler<CompoundButton.CheckedChangeEventArgs> OnCheckedChangeHandler;
            public EventHandler OnTipClickHandler;

            public RoadmapTaskViewHolder(View view) : base(view)
            {
                CbDone        = view.FindViewById<CheckBox>(Resource.Id.cb_roadmap_task_done);
                TvTitle       = view.FindViewById<TextView>(Resource.Id.tv_roadmap_task_title);
                TvDescription = view.FindViewById<TextView>(Resource.Id.tv_roadmap_task_description);
                TvDuration    = view.FindViewById<TextView>(Resource.Id.tv_roadmap_task_duration);
                BtnTip        = view.FindViewById<Button>(Resource.Id.btn_roadmap_task_tip);
                LlTips        = view.FindViewById<LinearLayout>(Resource.Id.ll_task_tips);
            }
        }
    }
}
