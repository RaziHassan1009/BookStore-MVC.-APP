using System;

namespace GrapheneSensore.Models
{
    /// <summary>
    /// Represents an active user session with security tracking
    /// </summary>
    public class UserSession
    {
        public Guid SessionId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
        public DateTime ExpirationTime { get; set; }
        public string? IpAddress { get; set; }
        public string? MachineName { get; set; }

        /// <summary>
        /// Checks if the session is still valid
        /// </summary>
        public bool IsValid()
        {
            return DateTime.UtcNow < ExpirationTime;
        }

        /// <summary>
        /// Updates the last activity time and extends the session
        /// </summary>
        public void UpdateActivity(int sessionTimeoutMinutes)
        {
            LastActivityTime = DateTime.UtcNow;
            ExpirationTime = DateTime.UtcNow.AddMinutes(sessionTimeoutMinutes);
        }
    }
}
