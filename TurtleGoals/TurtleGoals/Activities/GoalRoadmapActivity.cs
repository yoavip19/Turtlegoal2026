using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;

namespace TurtleGoals.Activities
{
    [Activity(Label = "Goal Roadmap", Theme = "@style/AppTheme")]
    public class GoalRoadmapActivity : AppCompatActivity
    {
        // TODO: Implement the full goal roadmap page.
        // The GoalId of the selected goal is passed via the Intent extra "goalId".

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_goal_roadmap);
        }
    }
}
