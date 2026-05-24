using System;
using System.Collections.Generic;
using Firebase.Firestore;
using TurtleGoals.Helpers;

namespace TurtleGoals.Models
{
    public class CommentModel
    {
        // ---------------------------------------------------------------
        // Fields
        // ---------------------------------------------------------------
        public string   CommentId { get; set; }  // document ID
        public string   GoalId    { get; set; }
        public string   TaskId    { get; set; }
        public string   UserId    { get; set; }
        public string   UserName  { get; set; }
        public string   Text      { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ---------------------------------------------------------------
        // Serialize → Firestore document
        // ---------------------------------------------------------------
        public Dictionary<string, Java.Lang.Object> ToDocument()
        {
            return new Dictionary<string, Java.Lang.Object>
            {
                { "goalId",    new Java.Lang.String(GoalId   ?? "") },
                { "taskId",    new Java.Lang.String(TaskId   ?? "") },
                { "userId",    new Java.Lang.String(UserId   ?? "") },
                { "userName",  new Java.Lang.String(UserName ?? "") },
                { "text",      new Java.Lang.String(Text     ?? "") },
                { "createdAt", FieldValue.ServerTimestamp() }
            };
        }

        // ---------------------------------------------------------------
        // Deserialize ← Firestore document
        // ---------------------------------------------------------------
        public static CommentModel FromDocument(string commentId, DocumentSnapshot doc)
        {
            return new CommentModel
            {
                CommentId = commentId,
                GoalId    = doc.GetString("goalId"),
                TaskId    = doc.GetString("taskId"),
                UserId    = doc.GetString("userId"),
                UserName  = doc.GetString("userName"),
                Text      = doc.GetString("text"),
                CreatedAt = DateHelper.ToCsharpDate(doc.GetDate("createdAt")) ?? DateTime.UtcNow
            };
        }
    }
}
