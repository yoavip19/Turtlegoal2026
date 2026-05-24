using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Util;
using Firebase.Firestore;
using Java.Util; // Added for HashMap
using Android.Gms.Extensions; // Added for AsAsync()
using TurtleGoals.Models;

namespace TurtleGoals.Helpers // Adjusted to match your previous namespace
{
    /// <summary>
    /// Central service for all Firestore operations.
    /// Collections are created automatically in Firestore the first time
    /// a document is written to them — no manual setup needed.
    /// </summary>
    public class FirestoreService
    {
        private static FirestoreService _instance;
        public static FirestoreService Instance => _instance ??= new FirestoreService();

        private readonly FirebaseFirestore _db;
        private const string TAG = "FirestoreService";

        // Collection name constants — avoids typos across the app
        private const string USERS_COLLECTION = "users";
        private const string GOALS_COLLECTION = "goals";
        private const string TASKS_SUBCOLLECTION = "tasks";
        private const string COMMENTS_COLLECTION = "comments";

        private FirestoreService()
        {
            // FIX: Grab the instance directly, no need for MainActivity.Db
            _db = FirebaseFirestore.Instance;
        }

        // ===================================================================
        // HELPER METHOD (C# Dictionary -> Java HashMap)
        // ===================================================================
        private HashMap ToJavaHashMap(Dictionary<string, Java.Lang.Object> dict)
        {
            var map = new HashMap();
            foreach (var item in dict)
            {
                map.Put(item.Key, item.Value);
            }
            return map;
        }

        // ===================================================================
        // USERS
        // ===================================================================

        public async Task<bool> CreateUser(UserModel user)
        {
            try
            {
                // FIX: Wrapped user.ToDocument() in our new ToJavaHashMap helper
                await _db.Collection(USERS_COLLECTION)
                         .Document(user.UserId)
                         .Set(ToJavaHashMap(user.ToDocument()))
                         .AsAsync();

                Log.Debug(TAG, $"User created: {user.UserId}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"CreateUser failed: {ex.Message}");
                return false;
            }
        }

        public async Task<UserModel> GetUser(string userId)
        {
            try
            {
                var doc = await _db.Collection(USERS_COLLECTION)
                                   .Document(userId)
                                   .Get()
                                   .AsAsync<DocumentSnapshot>();

                if (!doc.Exists())
                {
                    Log.Warn(TAG, $"User not found: {userId}");
                    return null;
                }

                return UserModel.FromDocument(userId, doc);
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"GetUser failed: {ex.Message}");
                return null;
            }
        }

        // ===================================================================
        // GOALS
        // ===================================================================

        public async Task<string> CreateGoal(GoalModel goal)
        {
            try
            {
                var goalRef = _db.Collection(GOALS_COLLECTION).Document();
                goal.GoalId = goalRef.Id;

                await goalRef.Set(ToJavaHashMap(goal.ToDocument())).AsAsync();
                Log.Debug(TAG, $"Goal created: {goal.GoalId}");

                foreach (var task in goal.Tasks)
                {
                    await CreateTask(goal.GoalId, task);
                }

                return goal.GoalId;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"CreateGoal failed: {ex.Message}");
                return null;
            }
        }

        public async Task<List<GoalModel>> GetUserGoals(string userId)
        {
            try
            {
                var query = await _db.Collection(GOALS_COLLECTION)
                                     .WhereEqualTo("userId", userId)
                                     .Get()
                                     .AsAsync<QuerySnapshot>();

                var goals = new List<GoalModel>();
                foreach (var doc in query.Documents)
                    goals.Add(GoalModel.FromDocument(doc.Id, doc));

                return goals;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"GetUserGoals failed: {ex.Message}");
                return new List<GoalModel>();
            }
        }

        public async Task<List<GoalModel>> GetPublicGoals()
        {
            try
            {
                var query = await _db.Collection(GOALS_COLLECTION)
                                     .WhereEqualTo("isPublic", true)
                                     .Get()
                                     .AsAsync<QuerySnapshot>();

                var goals = new List<GoalModel>();
                foreach (var doc in query.Documents)
                    goals.Add(GoalModel.FromDocument(doc.Id, doc));

                return goals;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"GetPublicGoals failed: {ex.Message}");
                return new List<GoalModel>();
            }
        }

        // ===================================================================
        // TASKS  (subcollection inside a goal)
        // ===================================================================

        public async Task<bool> CreateTask(string goalId, TaskModel task)
        {
            try
            {
                var taskRef = _db.Collection(GOALS_COLLECTION)
                                 .Document(goalId)
                                 .Collection(TASKS_SUBCOLLECTION)
                                 .Document();

                task.TaskId = taskRef.Id;
                await taskRef.Set(ToJavaHashMap(task.ToDocument())).AsAsync();

                Log.Debug(TAG, $"Task created: {task.TaskId} under goal {goalId}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"CreateTask failed: {ex.Message}");
                return false;
            }
        }

        public async Task<List<TaskModel>> GetGoalTasks(string goalId)
        {
            try
            {
                var query = await _db.Collection(GOALS_COLLECTION)
                                     .Document(goalId)
                                     .Collection(TASKS_SUBCOLLECTION)
                                     .OrderBy("order")
                                     .Get()
                                     .AsAsync<QuerySnapshot>();

                var tasks = new List<TaskModel>();
                foreach (var doc in query.Documents)
                    tasks.Add(TaskModel.FromDocument(doc.Id, doc));

                return tasks;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"GetGoalTasks failed: {ex.Message}");
                return new List<TaskModel>();
            }
        }

        public async Task<bool> SetTaskDone(string goalId, string taskId, bool isDone)
        {
            try
            {
                // FIX: Update() strictly requires a C# Dictionary, unlike Set()!
                var updates = new Dictionary<string, Java.Lang.Object>
                {
                    { "isDone", new Java.Lang.Boolean(isDone) }
                };

                if (isDone)
                    updates.Add("completedAt", FieldValue.ServerTimestamp());
                else
                    updates.Add("completedAt", null);

                await _db.Collection(GOALS_COLLECTION)
                         .Document(goalId)
                         .Collection(TASKS_SUBCOLLECTION)
                         .Document(taskId)
                         .Update(updates)
                         .AsAsync();

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"SetTaskDone failed: {ex.Message}");
                return false;
            }
        }

        // ===================================================================
        // COMMENTS
        // ===================================================================

        public async Task<bool> CreateComment(CommentModel comment)
        {
            try
            {
                var commentRef = _db.Collection(COMMENTS_COLLECTION).Document();
                comment.CommentId = commentRef.Id;

                await commentRef.Set(ToJavaHashMap(comment.ToDocument())).AsAsync();

                Log.Debug(TAG, $"Comment created: {comment.CommentId}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"CreateComment failed: {ex.Message}");
                return false;
            }
        }

        public async Task<List<CommentModel>> GetTaskComments(string taskId)
        {
            try
            {
                var query = await _db.Collection(COMMENTS_COLLECTION)
                                     .WhereEqualTo("taskId", taskId)
                                     .OrderBy("createdAt")
                                     .Get()
                                     .AsAsync<QuerySnapshot>();

                var comments = new List<CommentModel>();
                foreach (var doc in query.Documents)
                    comments.Add(CommentModel.FromDocument(doc.Id, doc));

                return comments;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"GetTaskComments failed: {ex.Message}");
                return new List<CommentModel>();
            }
        }
    }
}