using System;
using System.Collections.Generic;
using Firebase.Firestore;
using TurtleGoals.Helpers;
using static Google.Firestore.V1.StructuredQuery;

namespace TurtleGoals.Models
{
    public class GoalModel
    {
        // ---------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------
        public string    GoalId      { get; set; }  // document ID
        public string    UserId      { get; set; }
        public string    Title       { get; set; }
        public string    Description { get; set; }
        public DateTime? DueDate     { get; set; } = null;
        public bool      IsPublic    { get; set; } = true;
        public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; } = null;

        // Tasks are a subcollection in Firestore — loaded separately
        // but kept here for convenience when working in memory
        public List<TaskModel> Tasks { get; set; } = new List<TaskModel>();

        // ---------------------------------------------------------------
        // Serialize → Firestore document
        // ---------------------------------------------------------------
        public Dictionary<string, Java.Lang.Object> ToDocument()
        {
            var doc = new Dictionary<string, Java.Lang.Object>
            {
                { "userId",      new Java.Lang.String(UserId ?? "") },
                { "title",       new Java.Lang.String(Title ?? "") },
                { "description", new Java.Lang.String(Description ?? "") },
                { "isPublic",    new Java.Lang.Boolean(IsPublic) },
                { "createdAt",   FieldValue.ServerTimestamp() }
            };

            if (DueDate.HasValue)
                doc.Add("dueDate", DateHelper.ToJavaDate(DueDate));
            else
                doc.Add("dueDate", null);

            if (CompletedAt.HasValue)
                doc.Add("completedAt", DateHelper.ToJavaDate(CompletedAt));
            else
                doc.Add("completedAt", null);

            return doc;
        }

        // ---------------------------------------------------------------
        // Deserialize ← Firestore document
        // ---------------------------------------------------------------
        public static GoalModel FromDocument(string goalId, DocumentSnapshot doc)
        {
            return new GoalModel
            {
                GoalId = goalId,
                UserId = doc.GetString("userId"),
                Title = doc.GetString("title"),
                Description = doc.GetString("description"),
                IsPublic = doc.GetBoolean("isPublic")?.BooleanValue() ?? true,
                CreatedAt = DateHelper.ToCsharpDate(doc.GetDate("createdAt")) ?? DateTime.UtcNow,
                DueDate = DateHelper.ToCsharpDate(doc.GetDate("dueDate")),
                CompletedAt = DateHelper.ToCsharpDate(doc.GetDate("completedAt"))
            };
        }
    }
}
