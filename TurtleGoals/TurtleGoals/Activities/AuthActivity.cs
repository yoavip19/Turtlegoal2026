using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Firebase.Auth;
using Google.Android.Material.Button;
using Google.Android.Material.Tabs;
using Google.Android.Material.TextField;
using Android.Gms.Extensions;
using TurtleGoals.Helpers;
using TurtleGoals.Models;

namespace TurtleGoals.Activities
{
    [Activity(Label = "TurtleGoals", Theme = "@style/AppTheme", MainLauncher = true)]
    public class AuthActivity : AppCompatActivity
    {
        private TabLayout tabLayoutAuth;
        private TextInputLayout layoutFullName;
        private TextInputLayout layoutEmail;
        private TextInputLayout layoutPassword;
        private MaterialButton btnSubmit;
        private TextInputEditText etEmail, etPassword, etFullName;

        private FirebaseAuth firebaseAuth;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.auth_layout);

            // Initialize Firebase before any Firebase service is accessed
            Firebase.FirebaseApp.InitializeApp(this);

            // Initialize Firebase Auth
            firebaseAuth = FirebaseAuth.Instance;

            // If a user is already signed in, skip straight to the main screen
            if (firebaseAuth.CurrentUser != null)
            {
                NavigateToMain();
                return;
            }

            // Wire up views
            tabLayoutAuth  = FindViewById<TabLayout>(Resource.Id.tabLayoutAuth);
            layoutFullName = FindViewById<TextInputLayout>(Resource.Id.layoutFullName);
            layoutEmail    = FindViewById<TextInputLayout>(Resource.Id.layoutEmail);
            layoutPassword = FindViewById<TextInputLayout>(Resource.Id.layoutPassword);
            btnSubmit      = FindViewById<MaterialButton>(Resource.Id.btnSubmit);
            etEmail        = FindViewById<TextInputEditText>(Resource.Id.etEmail);
            etPassword     = FindViewById<TextInputEditText>(Resource.Id.etPassword);
            etFullName     = FindViewById<TextInputEditText>(Resource.Id.etFullName);

            // Tab switch: Login (position 0) / Register (position 1)
            tabLayoutAuth.TabSelected += (s, e) =>
            {
                ClearErrors();
                if (e.Tab.Position == 0)
                {
                    layoutFullName.Visibility = ViewStates.Gone;
                    btnSubmit.Text = "Let's Start!";
                }
                else
                {
                    layoutFullName.Visibility = ViewStates.Visible;
                    btnSubmit.Text = "Create Account";
                }
            };

            // Submit button — handles both Login and Register
            btnSubmit.Click += async (s, e) =>
            {
                if (!ValidateInputs())
                    return;

                string email    = etEmail.Text.Trim();
                string password = etPassword.Text; // intentionally not trimmed — whitespace may be part of the password

                btnSubmit.Enabled = false;

                try
                {
                    if (tabLayoutAuth.SelectedTabPosition == 0)
                    {
                        // ── Login ──────────────────────────────────────────
                        await firebaseAuth
                            .SignInWithEmailAndPassword(email, password)
                            .AsAsync<IAuthResult>();

                        NavigateToMain();
                    }
                    else
                    {
                        // ── Register ───────────────────────────────────────
                        string name = etFullName.Text.Trim();

                        var result = await firebaseAuth
                            .CreateUserWithEmailAndPassword(email, password)
                            .AsAsync<IAuthResult>();

                        // Persist the new user profile in Firestore
                        var newUser = new UserModel
                        {
                            UserId = result.User.Uid,
                            Name   = name,
                            Email  = email
                        };
                        await FirestoreService.Instance.CreateUser(newUser);

                        NavigateToMain();
                    }
                }
                catch (FirebaseAuthWeakPasswordException)
                {
                    layoutPassword.Error = "Password must be at least 6 characters";
                }
                catch (FirebaseAuthInvalidCredentialsException)
                {
                    layoutEmail.Error = "Invalid email or password";
                }
                catch (FirebaseAuthUserCollisionException)
                {
                    layoutEmail.Error = "An account with this email already exists";
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "Something went wrong: " + ex.Message, ToastLength.Long).Show();
                }
                finally
                {
                    btnSubmit.Enabled = true;
                }
            };
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool ValidateInputs()
        {
            ClearErrors();
            bool valid = true;

            if (tabLayoutAuth.SelectedTabPosition == 1 &&
                string.IsNullOrWhiteSpace(etFullName.Text))
            {
                layoutFullName.Error = "Please enter your name";
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(etEmail.Text))
            {
                layoutEmail.Error = "Please enter your email";
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(etPassword.Text))
            {
                layoutPassword.Error = "Please enter your password";
                valid = false;
            }
            else if (etPassword.Text.Length < 6)
            {
                layoutPassword.Error = "Password must be at least 6 characters";
                valid = false;
            }

            return valid;
        }

        private void ClearErrors()
        {
            layoutFullName.Error = null;
            layoutEmail.Error    = null;
            layoutPassword.Error = null;
        }

        private void NavigateToMain()
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
            StartActivity(intent);
            Finish();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [Android.Runtime.GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
