using System;
using System.Collections.Generic;
using Firebase.Firestore;
using TurtleGoals.Helpers;

namespace TurtleGoals.Models
{
    public class UserModel
    {
        // ---------------------------------------------------------------
        // Fields (match Firestore document fields exactly)
        // ---------------------------------------------------------------
        public string UserId    { get; set; }   // document ID, not stored as a field
        public string Name      { get; set; }
        public string Email     { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ---------------------------------------------------------------
        // Serialize → Firestore document
        // ---------------------------------------------------------------
        public Dictionary<string, Java.Lang.Object> ToDocument()
        {
            return new Dictionary<string, Java.Lang.Object>
            {
                { "name",      new Java.Lang.String(Name  ?? "") },
                { "email",     new Java.Lang.String(Email ?? "") },
                { "createdAt", FieldValue.ServerTimestamp() }
            };
        }

        // ---------------------------------------------------------------
        // Deserialize ← Firestore document
        // ---------------------------------------------------------------
        public static UserModel FromDocument(string userId, DocumentSnapshot doc)
        {
            return new UserModel
            {
                UserId    = userId,
                Name      = doc.GetString("name"),
                Email     = doc.GetString("email"),
                CreatedAt = DateHelper.ToCsharpDate(doc.GetDate("createdAt")) ?? DateTime.UtcNow
            };
        }
    }
}
