using System;
using System.Collections.Generic;
using Firebase.Firestore;
using TurtleGoals.Helpers;

namespace TurtleGoals.Models
{
    public class TaskModel
    {
        // ---------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------
        public string   TaskId       { get; set; }  // document ID
        public int      Order        { get; set; }
        public string   Title        { get; set; }
        public string   Description  { get; set; }
        public int      DurationDays { get; set; }
        public bool     IsDone       { get; set; } = false;
        public DateTime? CompletedAt { get; set; } = null;

        // ---------------------------------------------------------------
        // Serialize → Firestore document
        // ---------------------------------------------------------------
        public Dictionary<string, Java.Lang.Object> ToDocument()
        {
            var doc = new Dictionary<string, Java.Lang.Object>
            {
                { "order",        new Java.Lang.Integer(Order) },
                { "title",        new Java.Lang.String(Title ?? "") },
                { "description",  new Java.Lang.String(Description ?? "") },
                { "durationDays", new Java.Lang.Integer(DurationDays) },
                { "isDone",       new Java.Lang.Boolean(IsDone) }
            };

            if (IsDone)
                doc.Add("completedAt", FieldValue.ServerTimestamp()); // The correct native timestamp
            else
                doc.Add("completedAt", null);

            return doc;
        }

        // ---------------------------------------------------------------
        // Deserialize ← Firestore document
        // ---------------------------------------------------------------
        public static TaskModel FromDocument(string taskId, DocumentSnapshot doc)
        {
            return new TaskModel
            {
                TaskId = taskId,
                Order = doc.GetLong("order")?.IntValue() ?? 0,
                Title = doc.GetString("title"),
                Description = doc.GetString("description"),
                DurationDays = doc.GetLong("durationDays")?.IntValue() ?? 0,
                IsDone = doc.GetBoolean("isDone")?.BooleanValue() ?? false,
                CompletedAt = DateHelper.ToCsharpDate(doc.GetDate("completedAt"))
            };
        }
    }
}
