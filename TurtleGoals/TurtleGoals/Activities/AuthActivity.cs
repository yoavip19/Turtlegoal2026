using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.Tabs;
using Google.Android.Material.TextField;
using Google.Android.Material.Button;
using Android;

namespace TurtleGoals.Activities
{
    [Activity(Label = "TurtleGoals", Theme = "@style/AppTheme")]
    public class AuthActivity : AppCompatActivity
    {
        private TabLayout tabLayoutAuth;
        private TextInputLayout layoutFullName;
        private MaterialButton btnSubmit;
        private TextInputEditText etEmail, etPassword, etFullName;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.auth_layout); // אתחול רכיבים
            tabLayoutAuth = FindViewById<TabLayout>(Resource.Id.tabLayoutAuth); 
            layoutFullName = FindViewById<TextInputLayout>(Resource.Id.layoutFullName); 
            btnSubmit = FindViewById<MaterialButton>(Resource.Id.btnSubmit); 
            etEmail = FindViewById<TextInputEditText>(Resource.Id.etEmail); 
            etPassword = FindViewById<TextInputEditText>(Resource.Id.etPassword); 
            etFullName = FindViewById<TextInputEditText>(Resource.Id.etFullName); // האזנה לשינוי טאבים (Login/Register)
            tabLayoutAuth.TabSelected += (s, e) => { 
            if (e.Tab.Position == 0) { // מצב Login
                layoutFullName.Visibility = ViewStates.Gone; 
                btnSubmit.Text = "Let's Start!"; } 
            else { // מצב Register
                layoutFullName.Visibility = ViewStates.Visible; btnSubmit.Text = "Create Account"; } }; // לחיצה על הכפתור הראשי
             btnSubmit.Click += (s, e) => { string userEmail = etEmail.Text; if (tabLayoutAuth.SelectedTabPosition == 0) { // סימולציה של התחברות
                           Toast.MakeText(this, $"Welcome back, {userEmail}!", ToastLength.Short).Show(); } else { // סימולציה של הרשמה
                                                               string name = etFullName.Text; Toast.MakeText(this, $"Account created for {name}!", ToastLength.Short).Show(); } 
             }; 
        } 
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [Android.Runtime.GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        { Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults); base.OnRequestPermissionsResult(requestCode, permissions, grantResults); } } }  
      