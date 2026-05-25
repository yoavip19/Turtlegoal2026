using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using TurtleGoals.Activities;
using TurtleGoals.Models;

namespace TurtleGoals.Helpers
{
    public class GoalBannerAdapter : RecyclerView.Adapter
    {
        private readonly Context _context;
        private List<GoalModel> _goals;

        public GoalBannerAdapter(Context context, List<GoalModel> goals)
        {
            _context = context;
            _goals = goals ?? new List<GoalModel>();
        }

        public void UpdateGoals(List<GoalModel> goals)
        {
            _goals = goals ?? new List<GoalModel>();
            NotifyDataSetChanged();
        }

        public override int ItemCount => _goals.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.goal_item, parent, false);
            return new GoalViewHolder(view, OnGoalClicked);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = (GoalViewHolder)holder;
            vh.TvTitle.Text = _goals[position].Title;
        }

        private void OnGoalClicked(int position)
        {
            if (position < 0 || position >= _goals.Count) return;
            var intent = new Intent(_context, typeof(GoalRoadmapActivity));
            intent.PutExtra("goalId", _goals[position].GoalId);
            _context.StartActivity(intent);
        }

        private class GoalViewHolder : RecyclerView.ViewHolder
        {
            public TextView TvTitle { get; }

            public GoalViewHolder(View view, System.Action<int> onClick) : base(view)
            {
                TvTitle = view.FindViewById<TextView>(Resource.Id.tvGoalItemTitle);
                view.Click += (s, e) => onClick(AdapterPosition);
            }
        }
    }
}
