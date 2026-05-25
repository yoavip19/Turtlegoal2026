using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using TurtleGoals.Activities;
using TurtleGoals.Models;

namespace TurtleGoals.Adapters
{
    public class CommunityFeedAdapter : RecyclerView.Adapter
    {
        private List<GoalModel> _allGoals;
        private List<GoalModel> _filteredGoals;

        public CommunityFeedAdapter(List<GoalModel> goals)
        {
            _allGoals = goals ?? new List<GoalModel>();
            _filteredGoals = new List<GoalModel>(_allGoals);
        }

        public override int ItemCount => _filteredGoals.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context)
                                     .Inflate(Resource.Layout.goal_card, parent, false);
            return new GoalViewHolder(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = (GoalViewHolder)holder;
            var goal = _filteredGoals[position];

            vh.TvTitle.Text = goal.Title ?? "Untitled Goal";

            if (!string.IsNullOrWhiteSpace(goal.Description))
            {
                vh.TvDescription.Visibility = ViewStates.Visible;
                vh.TvDescription.Text = goal.Description;
            }
            else
            {
                vh.TvDescription.Visibility = ViewStates.Gone;
            }

            // Author: show first 8 chars of userId for privacy
            var authorLabel = !string.IsNullOrEmpty(goal.UserId) && goal.UserId.Length >= 8
                ? goal.UserId.Substring(0, 8) + "…"
                : (goal.UserId ?? "Unknown");
            vh.TvAuthor.Text = authorLabel;

            // Created date
            vh.TvCreatedDate.Text = goal.CreatedAt.ToString("MMM d, yyyy");

            // Due date
            if (goal.DueDate.HasValue)
            {
                vh.LlDueDate.Visibility = ViewStates.Visible;
                vh.TvDueDate.Text = goal.DueDate.Value.ToString("MMM d, yyyy");
            }
            else
            {
                vh.LlDueDate.Visibility = ViewStates.Gone;
            }

            // Status badge
            if (goal.CompletedAt.HasValue)
            {
                vh.TvStatusBadge.Text = "Completed";
                vh.TvStatusBadge.SetBackgroundResource(Resource.Drawable.badge_background);
            }
            else
            {
                vh.TvStatusBadge.Text = "Active";
                vh.TvStatusBadge.SetBackgroundResource(Resource.Drawable.badge_active_background);
            }

            // Update GoalId so the click handler (wired once in the constructor) uses the right ID
            vh.GoalId = goal.GoalId;
        }

        /// <summary>
        /// Filters the displayed goals by a search query (matches title or description).
        /// </summary>
        public void Filter(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _filteredGoals = new List<GoalModel>(_allGoals);
            }
            else
            {
                var lower = query.ToLowerInvariant();
                _filteredGoals = _allGoals
                    .Where(g => (g.Title ?? "").ToLowerInvariant().Contains(lower)
                             || (g.Description ?? "").ToLowerInvariant().Contains(lower))
                    .ToList();
            }
            NotifyDataSetChanged();
        }

        /// <summary>
        /// Replaces the full data set (called after a fresh Firestore fetch).
        /// </summary>
        public void UpdateData(List<GoalModel> goals)
        {
            _allGoals = goals ?? new List<GoalModel>();
            _filteredGoals = new List<GoalModel>(_allGoals);
            NotifyDataSetChanged();
        }

        // ── ViewHolder ────────────────────────────────────────────────────────

        public class GoalViewHolder : RecyclerView.ViewHolder
        {
            public TextView TvTitle         { get; }
            public TextView TvDescription   { get; }
            public TextView TvAuthor        { get; }
            public TextView TvCreatedDate   { get; }
            public TextView TvDueDate       { get; }
            public LinearLayout LlDueDate   { get; }
            public TextView TvStatusBadge   { get; }
            public Android.Widget.Button BtnInspect { get; }

            public string GoalId { get; set; }

            public GoalViewHolder(View view) : base(view)
            {
                TvTitle       = view.FindViewById<TextView>(Resource.Id.tv_goal_title);
                TvDescription = view.FindViewById<TextView>(Resource.Id.tv_goal_description);
                TvAuthor      = view.FindViewById<TextView>(Resource.Id.tv_author);
                TvCreatedDate = view.FindViewById<TextView>(Resource.Id.tv_created_date);
                TvDueDate     = view.FindViewById<TextView>(Resource.Id.tv_due_date);
                LlDueDate     = view.FindViewById<LinearLayout>(Resource.Id.ll_due_date);
                TvStatusBadge = view.FindViewById<TextView>(Resource.Id.tv_status_badge);
                BtnInspect    = view.FindViewById<Android.Widget.Button>(Resource.Id.btn_inspect);

                // Wire the Inspect click once — GoalId is updated in OnBindViewHolder
                BtnInspect.Click += OnInspectClick;
            }

            public void OnInspectClick(object sender, EventArgs e)
            {
                var intent = new Android.Content.Intent(ItemView.Context, typeof(GoalRoadmapActivity));
                intent.PutExtra("goalId", GoalId);
                ItemView.Context.StartActivity(intent);
            }
        }
    }
}
