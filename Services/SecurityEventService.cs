using System;
using System.Data.Entity;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>
    /// Writes to and reads from SecurityEvent - the access/account-management audit trail. Closes
    /// the gap the system originally shipped with, where sign-outs, failed sign-ins and account
    /// edits went unrecorded (see the old remarks on ActivityService). AuthService and UserService
    /// call <see cref="Log"/>; ActivityService reads it back for the Admin activity feed.
    /// </summary>
    public static class SecurityEventService
    {
        /// <summary>
        /// Records one event. Never throws into the caller's success/failure path - a logging
        /// failure must not turn a successful sign-in or account edit into an error page.
        /// </summary>
        public static void Log(string eventType, string username, int? userId, int? actorUserId, string detail)
        {
            try
            {
                using (var db = new SmartShelfContext())
                {
                    db.SecurityEvents.Add(new SecurityEvent
                    {
                        EventType = eventType,
                        Username = string.IsNullOrWhiteSpace(username) ? "(unknown)" : username.Trim(),
                        UserId = userId,
                        ActorUserId = actorUserId,
                        Detail = detail,
                        CreatedAt = DateTime.Now
                    });
                    db.SaveChanges();
                }
            }
            catch
            {
                // Auditing is best-effort: a DB hiccup here must not block sign-in/out or account
                // management, which have already completed by the time this is called.
            }
        }

        /// <summary>Used by ActivityService to build the Admin activity feed's Access rows.</summary>
        public static System.Collections.Generic.List<SecurityEvent> GetEvents(
            DateTime? start, DateTime? end, int? userId, int cap)
        {
            using (var db = new SmartShelfContext())
            {
                var q = db.SecurityEvents.AsNoTracking()
                    .Include(x => x.User)
                    .Include(x => x.User.Role)
                    .Include(x => x.ActorUser)
                    .Include(x => x.ActorUser.Role)
                    .AsQueryable();

                if (start.HasValue) q = q.Where(x => x.CreatedAt >= start.Value);
                if (end.HasValue) q = q.Where(x => x.CreatedAt < end.Value);
                if (userId.HasValue) q = q.Where(x => x.UserId == userId.Value || x.ActorUserId == userId.Value);

                return q.OrderByDescending(x => x.CreatedAt).Take(cap).ToList();
            }
        }
    }
}
