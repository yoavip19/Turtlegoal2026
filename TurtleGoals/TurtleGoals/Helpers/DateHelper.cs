using System;
using Java.Util;

namespace TurtleGoals.Helpers
{
    public static class DateHelper
    {
        // Converts Java Date from Firestore into C# DateTime
        public static DateTime? ToCsharpDate(Date javaDate)
        {
            if (javaDate == null) return null;
            return DateTimeOffset.FromUnixTimeMilliseconds(javaDate.Time).UtcDateTime;
        }

        // Converts C# DateTime to Java Date for Firestore
        public static Date ToJavaDate(DateTime? csharpDate)
        {
            if (!csharpDate.HasValue) return null;
            long ms = new DateTimeOffset(csharpDate.Value).ToUnixTimeMilliseconds();
            return new Date(ms);
        }
    }
}