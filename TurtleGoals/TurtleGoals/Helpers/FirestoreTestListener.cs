using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Tasks;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TurtleGoals.Helpers
{
    public class FirestoreTestListener : Java.Lang.Object, IOnCompleteListener
    {
        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            if (task.IsSuccessful)
            {
                // IT WORKED!
                System.Diagnostics.Debug.WriteLine("QUAKE!========= TURTLE-GOAL SUCCESS =========");
                System.Diagnostics.Debug.WriteLine("QUAKE!Mock collection created and document saved!");
            }
            else
            {
                // IT FAILED! Check your google-services.json or internet connection
                System.Diagnostics.Debug.WriteLine("QUAKE!========= TURTLE-GOAL ERROR =========");
                System.Diagnostics.Debug.WriteLine($"QUAKE!Connection failed: {task.Exception?.Message}");
            }
        }
    }
}