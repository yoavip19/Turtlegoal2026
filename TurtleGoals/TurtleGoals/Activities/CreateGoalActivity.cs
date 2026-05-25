using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Firebase.Auth;
using TurtleGoals.Helpers;
using TurtleGoals.Models;

namespace TurtleGoals.Activities
{
    [Activity(Label = "Create a Goal", Theme = "@style/AppTheme")]
    public class CreateGoalActivity : AppCompatActivity
    {
        // ===== API CONFIG =====
        private const string ApiKey = "AIzaSyDwSgTgZMeAp5qoIXlVssZrni0H9k8s3fk";
        private const string ApiUrl =
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=";

        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        // ===== UI references =====
        private TextInputEditText _goalTitleInput;
        private TextInputEditText _goalInput;
        private MaterialButton _datePickerButton;
        private TextView _selectedDateText;
        private MaterialButton _generateButton;
        private ProgressBar _loadingSpinner;
        private TextView _goalTitleText;
        private TextView _weeksEstimateText;
        private LinearLayout _tasksContainer;
        private ScrollView _resultsScroll;
        private MaterialButton _btnBack;
        private MaterialButton _btnSaveGoal;

        private DateTime? _selectedDate;
        private JObject _generatedPlan;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_create_goal);

            _goalTitleInput   = FindViewById<TextInputEditText>(Resource.Id.goalTitleInput);
            _goalInput        = FindViewById<TextInputEditText>(Resource.Id.goalInput);
            _datePickerButton = FindViewById<MaterialButton>(Resource.Id.datePickerButton);
            _selectedDateText = FindViewById<TextView>(Resource.Id.selectedDateText);
            _generateButton   = FindViewById<MaterialButton>(Resource.Id.generateButton);
            _loadingSpinner   = FindViewById<ProgressBar>(Resource.Id.loadingSpinner);
            _goalTitleText    = FindViewById<TextView>(Resource.Id.goalTitleText);
            _weeksEstimateText = FindViewById<TextView>(Resource.Id.weeksEstimateText);
            _tasksContainer   = FindViewById<LinearLayout>(Resource.Id.tasksContainer);
            _resultsScroll    = FindViewById<ScrollView>(Resource.Id.resultsScroll);
            _btnBack          = FindViewById<MaterialButton>(Resource.Id.btnBack);
            _btnSaveGoal      = FindViewById<MaterialButton>(Resource.Id.btnSaveGoal);

            _btnBack.Click += (s, e) => Finish();
            _datePickerButton.Click += OnDatePickerClicked;
            _generateButton.Click += async (s, e) => await OnGenerateClickedAsync();
            _btnSaveGoal.Click += async (s, e) => await OnSaveGoalClickedAsync();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        // ===== Date picker =====
        private void OnDatePickerClicked(object sender, EventArgs e)
        {
            var initial = _selectedDate ?? DateTime.Today;
            var dialog = new DatePickerDialog(this, (s, args) =>
            {
                _selectedDate = args.Date;
                _selectedDateText.Text = $"📅 Due: {args.Date:dd/MM/yyyy}";
                _selectedDateText.Visibility = ViewStates.Visible;
                _datePickerButton.Text = $"📅 {args.Date:dd/MM/yyyy}";
            }, initial.Year, initial.Month - 1, initial.Day);
            dialog.Show();
        }

        // ===== Generate handler =====
        private async Task OnGenerateClickedAsync()
        {
            var goalTitle = _goalTitleInput.Text?.Trim();
            var goalDesc = _goalInput.Text?.Trim();

            if (string.IsNullOrEmpty(goalTitle))
            {
                Toast.MakeText(this, "Please enter a goal title", ToastLength.Short).Show();
                return;
            }
            if (string.IsNullOrEmpty(goalDesc))
            {
                Toast.MakeText(this, "Please describe your goal", ToastLength.Short).Show();
                return;
            }

            // Reset previous results
            _tasksContainer.RemoveAllViews();
            _goalTitleText.Visibility = ViewStates.Gone;
            _weeksEstimateText.Visibility = ViewStates.Gone;
            _resultsScroll.Visibility = ViewStates.Gone;
            _btnSaveGoal.Visibility = ViewStates.Gone;
            _loadingSpinner.Visibility = ViewStates.Visible;
            _generateButton.Enabled = false;

            try
            {
                var timeframe = _selectedDate.HasValue
                    ? _selectedDate.Value.ToString("dd/MM/yyyy")
                    : "No specific deadline";

                var prompt = BuildPrompt(goalTitle, goalDesc, timeframe);
                var modelText = await CallGeminiAsync(prompt);
                var cleanedJson = ExtractJson(modelText);
                DisplayPlan(cleanedJson);
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Error: {ex.Message}", ToastLength.Long).Show();
            }
            finally
            {
                _loadingSpinner.Visibility = ViewStates.Gone;
                _generateButton.Enabled = true;
            }
        }

        // ===== Save goal to Firestore =====
        private async Task OnSaveGoalClickedAsync()
        {
            if (_generatedPlan == null) return;

            var userId = FirebaseAuth.Instance.CurrentUser?.Uid;
            if (string.IsNullOrEmpty(userId))
            {
                Toast.MakeText(this, "You must be signed in to save a goal", ToastLength.Short).Show();
                return;
            }

            _btnSaveGoal.Enabled = false;

            try
            {
                var goal = new GoalModel
                {
                    UserId = userId,
                    Title = _generatedPlan["goal_title"]?.ToString() ?? _goalTitleInput.Text?.Trim(),
                    Description = _goalInput.Text?.Trim() ?? string.Empty,
                    DueDate = _selectedDate,
                    IsPublic = true
                };

                // Build task list from generated plan
                var tasksArray = _generatedPlan["tasks"] as JArray;
                if (tasksArray != null)
                {
                    int order = 0;
                    foreach (var taskJson in tasksArray)
                    {
                        goal.Tasks.Add(new TaskModel
                        {
                            Order = order,
                            Title = taskJson["title"]?.ToString() ?? $"Task {order + 1}",
                            Description = taskJson["description"]?.ToString() ?? string.Empty,
                            DurationDays = taskJson["duration_days"]?.Value<int>() ?? 7,
                            IsDone = false
                        });
                        order++;
                    }
                }

                await FirestoreService.Instance.CreateGoal(goal);
                Toast.MakeText(this, "Goal saved! 🐢", ToastLength.Short).Show();

                // Return to dashboard
                SetResult(Result.Ok);
                Finish();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Could not save goal: {ex.Message}", ToastLength.Long).Show();
            }
            finally
            {
                RunOnUiThread(() => _btnSaveGoal.Enabled = true);
            }
        }

        // ===== Prompt builder =====
        private string BuildPrompt(string title, string description, string timeframe)
        {
            return
                "# Role: TurtleGoal AI Engine (Optimized for Gemini 2.5 Flash)\n" +
                "You are a goal-shredding expert specializing in \"Atomic Tasks\" and the \"Tortoise Approach.\"\n" +
                "Your mission: Transform overwhelming goals into a series of tiny, achievable \"Turtle Steps\" that prioritize consistency over speed.\n\n" +

                "# Operational Rules:\n" +
                "1. Break the goal into 6-8 tasks only.\n" +
                "2. The \"5-Minute Rule\": Task #1 MUST be a \"micro-win\" that takes less than 5 minutes to complete.\n" +
                "3. \"The Tortoise Logic\": No single task should have a duration_days greater than 14. Focus descriptions on small daily repetitions (e.g., \"Read 2 pages daily\" instead of \"Finish the chapter\").\n" +
                "4. \"Resource Integration\": You MUST actively incorporate the provided [RESOURCES] into the task descriptions. Use the specific tools, budgets, or environments mentioned by the user.\n" +
                "5. \"Skill Variety\": Ensure the roadmap alternates between different types of activities (e.g., theory, practical exercise, consumption, and production) to maintain user engagement and avoid burnout.\n" +
                "6. \"Smart Tips\": The contextual_tip must provide a \"pro-hack\" or a clever shortcut related to the specific task (e.g., mnemonic devices or efficiency tips) rather than just general encouragement.\n" +
                "7. Language: Output ALL text values in English.\n" +
                "8. Output: Return ONLY a raw JSON object. No markdown, no \"```json\", no intro/outro text.\n\n" +

                "# JSON Structure:\n" +
                "{\n" +
                "  \"goal_title\": \"string\",\n" +
                "  \"total_weeks_estimate\": integer,\n" +
                "  \"tasks\": [\n" +
                "    {\n" +
                "      \"id\": integer,\n" +
                "      \"title\": \"short name (max 5 words)\",\n" +
                "      \"description\": \"very specific, low-friction instruction\",\n" +
                "      \"duration_days\": integer,\n" +
                "      \"minutes_per_session\": integer,\n" +
                "      \"difficulty_level\": \"integer (1-5)\",\n" +
                "      \"emoji\": \"relevant emoji\",\n" +
                "      \"contextual_tip\": \"A Tortoise-style clever hack or shortcut for this specific step\",\n" +
                "      \"victory_message\": \"A short, encouraging cheer for completing this step\"\n" +
                "    }\n" +
                "  ]\n" +
                "}\n\n" +

                "# Goal Inputs:\n" +
                $"User Goal: {title} — {description}\n" +
                $"Timeframe: {timeframe}\n" +
                "Resources: none";
        }

        // ===== Gemini API call =====
        private async Task<string> CallGeminiAsync(string prompt)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiUrl + ApiKey, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"API error ({response.StatusCode}): {responseText}");

            var responseObj = JObject.Parse(responseText);
            var text = responseObj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

            if (string.IsNullOrEmpty(text))
                throw new Exception("No content returned from Gemini API");

            return text;
        }

        // ===== Extract JSON from model response =====
        private string ExtractJson(string modelText)
        {
            // Strip markdown code fences if present
            var cleaned = modelText.Trim();
            if (cleaned.StartsWith("```json"))
                cleaned = cleaned.Substring(7);
            else if (cleaned.StartsWith("```"))
                cleaned = cleaned.Substring(3);

            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3);

            return cleaned.Trim();
        }

        // ===== Display the generated plan =====
        private void DisplayPlan(string jsonText)
        {
            _generatedPlan = JObject.Parse(jsonText);

            var goalTitle = _generatedPlan["goal_title"]?.ToString() ?? "Your Goal";
            var weeksEstimate = _generatedPlan["total_weeks_estimate"]?.Value<int>() ?? 0;

            _goalTitleText.Text = $"🐢 {goalTitle}";
            _goalTitleText.Visibility = ViewStates.Visible;

            _weeksEstimateText.Text = $"⏱️ Estimated: {weeksEstimate} weeks";
            _weeksEstimateText.Visibility = ViewStates.Visible;

            _resultsScroll.Visibility = ViewStates.Visible;

            var tasks = _generatedPlan["tasks"] as JArray;
            if (tasks == null) return;

            _tasksContainer.RemoveAllViews();

            foreach (var task in tasks)
            {
                AddTaskCard(task);
            }

            _btnSaveGoal.Visibility = ViewStates.Visible;
        }

        // ===== Add a task card to the UI =====
        private void AddTaskCard(JToken task)
        {
            int dp12 = (int)(12 * Resources.DisplayMetrics.Density);
            int dp16 = (int)(16 * Resources.DisplayMetrics.Density);
            int dp8 = (int)(8 * Resources.DisplayMetrics.Density);

            // Card container
            var card = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical
            };
            card.SetBackgroundColor(Android.Graphics.Color.White);
            card.SetPadding(dp16, dp16, dp16, dp16);

            var layoutParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent,
                LinearLayout.LayoutParams.WrapContent);
            layoutParams.SetMargins(0, 0, 0, dp12);
            card.LayoutParameters = layoutParams;

            // Emoji + Title
            var emoji = task["emoji"]?.ToString() ?? "🐢";
            var title = task["title"]?.ToString() ?? "Task";
            var titleView = new TextView(this)
            {
                Text = $"{emoji} {title}",
                TextSize = 16
            };
            titleView.SetTextColor(Android.Graphics.Color.ParseColor("#6D574F"));
            titleView.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
            card.AddView(titleView);

            // Description
            var desc = task["description"]?.ToString();
            if (!string.IsNullOrEmpty(desc))
            {
                var descView = new TextView(this) { Text = desc, TextSize = 14 };
                descView.SetTextColor(Android.Graphics.Color.ParseColor("#4F4541"));
                descView.SetPadding(0, dp8, 0, 0);
                card.AddView(descView);
            }

            // Duration + Minutes per session
            var duration = task["duration_days"]?.Value<int>() ?? 0;
            var minutes = task["minutes_per_session"]?.Value<int>() ?? 0;
            var metaView = new TextView(this)
            {
                Text = $"📆 {duration} days  •  ⏰ {minutes} min/session",
                TextSize = 12
            };
            metaView.SetTextColor(Android.Graphics.Color.ParseColor("#9E8880"));
            metaView.SetPadding(0, dp8, 0, 0);
            card.AddView(metaView);

            // Contextual tip
            var tip = task["contextual_tip"]?.ToString();
            if (!string.IsNullOrEmpty(tip))
            {
                var tipView = new TextView(this)
                {
                    Text = $"💡 {tip}",
                    TextSize = 13
                };
                tipView.SetTextColor(Android.Graphics.Color.ParseColor("#286B33"));
                tipView.SetPadding(0, dp8, 0, 0);
                card.AddView(tipView);
            }

            _tasksContainer.AddView(card);
        }
    }
}
