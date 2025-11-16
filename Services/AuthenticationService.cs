using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Helpers;
using GrapheneSensore.Logging;
using GrapheneSensore.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete

namespace GrapheneSensore.Services
{
    /// <summary>
    /// Provides secure authentication and session management services
    /// </summary>
    public class AuthenticationService
    {
        private UserSession? _currentSession;
        private readonly object _sessionLock = new object();

        /// <summary>
        /// Gets the current authenticated user
        /// </summary>
        public User? CurrentUser { get; private set; }

        /// <summary>
        /// Gets the current user session
        /// </summary>
        public UserSession? CurrentSession
        {
            get
            {
                lock (_sessionLock)
                {
                    if (_currentSession != null && !_currentSession.IsValid())
                    {
                        // Session expired
                        Logout();
                        return null;
                    }
                    return _currentSession;
                }
            }
        }

        /// <summary>
        /// Indicates whether a user is currently authenticated
        /// </summary>
        public bool IsAuthenticated => CurrentUser != null && CurrentSession != null && CurrentSession.IsValid();

        /// <summary>
        /// Authenticates a user and creates a secure session
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <returns>Tuple containing success status, message, and user object</returns>
        public async Task<(bool success, string message, User? user)> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Logger.Instance.LogWarning("Login attempt with empty credentials", "Auth");
                return (false, "Username and password are required", null);
            }

            try
            {
                Logger.Instance.LogInfo($"Login attempt for user: {username}", "Auth");
                
                using var context = new SensoreDbContext();
                var user = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    Logger.Instance.LogWarning($"Login failed - user not found: {username}", "Auth");
                    // Use generic message to prevent username enumeration
                    return (false, "Invalid username or password", null);
                }

                if (!user.IsActive)
                {
                    Logger.Instance.LogWarning($"Login failed - user inactive: {username}", "Auth");
                    return (false, "Account is inactive. Please contact administrator.", null);
                }

                // Verify password using PasswordHelper
                bool passwordValid = PasswordHelper.VerifyPassword(password, user.PasswordHash);
                
                if (!passwordValid)
                {
                    Logger.Instance.LogWarning($"Login failed - invalid password for user: {username}", "Auth");
                    // Use generic message to prevent username enumeration
                    return (false, "Invalid username or password", null);
                }

                // Update last login date
                using var updateContext = new SensoreDbContext();
                var userToUpdate = await updateContext.Users.FindAsync(user.UserId);
                if (userToUpdate != null)
                {
                    userToUpdate.LastLoginDate = DateTime.UtcNow;
                    await updateContext.SaveChangesAsync();
                }

                // Create secure session
                CreateSession(user);

                CurrentUser = user;
                Logger.Instance.LogInfo($"Login successful for user: {username} (Type: {user.UserType})", "Auth");
                return (true, "Login successful", user);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Login error for user: {username}", ex, "Auth");
                return (false, "An error occurred during login. Please try again.", null);
            }
        }

        /// <summary>
        /// Logs out the current user and invalidates the session
        /// </summary>
        public void Logout()
        {
            lock (_sessionLock)
            {
                if (CurrentUser != null)
                {
                    Logger.Instance.LogInfo($"User logged out: {CurrentUser.Username}", "Auth");
                }
                CurrentUser = null;
                _currentSession = null;
            }
        }

        /// <summary>
        /// Creates a new session for the authenticated user
        /// </summary>
        private void CreateSession(User user)
        {
            lock (_sessionLock)
            {
                var config = AppConfiguration.Instance;
                _currentSession = new UserSession
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    UserType = user.UserType,
                    LoginTime = DateTime.UtcNow,
                    LastActivityTime = DateTime.UtcNow,
                    ExpirationTime = DateTime.UtcNow.AddMinutes(config.SessionTimeoutMinutes),
                    MachineName = Environment.MachineName
                };
            }
        }

        /// <summary>
        /// Updates the current session activity timestamp
        /// </summary>
        public void UpdateSessionActivity()
        {
            lock (_sessionLock)
            {
                if (_currentSession != null && _currentSession.IsValid())
                {
                    var config = AppConfiguration.Instance;
                    _currentSession.UpdateActivity(config.SessionTimeoutMinutes);
                }
            }
        }

        /// <summary>
        /// Changes a user's password with validation
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="oldPassword">The current password</param>
        /// <param name="newPassword">The new password</param>
        /// <returns>Tuple containing success status and message</returns>
        public async Task<(bool success, string message)> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "Passwords cannot be empty");
            }

            if (oldPassword == newPassword)
            {
                return (false, "New password must be different from the current password");
            }

            try
            {
                // Validate new password
                var (isValid, validationMessage) = PasswordHelper.ValidatePassword(newPassword);
                if (!isValid)
                {
                    return (false, validationMessage);
                }

                using var context = new SensoreDbContext();
                var user = await context.Users.FindAsync(userId);

                if (user == null)
                {
                    Logger.Instance.LogWarning($"Password change failed - user not found: {userId}", "Auth");
                    return (false, "User not found");
                }

                if (!PasswordHelper.VerifyPassword(oldPassword, user.PasswordHash))
                {
                    Logger.Instance.LogWarning($"Password change failed - incorrect old password for user: {user.Username}", "Auth");
                    return (false, "Current password is incorrect");
                }

                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Password changed successfully for user: {user.Username}", "Auth");
                return (true, "Password changed successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error changing password for user: {userId}", ex, "Auth");
                return (false, "An error occurred while changing the password. Please try again.");
            }
        }

        /// <summary>
        /// Checks if the current user has the required permission level
        /// </summary>
        /// <param name="requiredUserType">The required user type</param>
        /// <returns>True if user has permission, false otherwise</returns>
        public bool HasPermission(string requiredUserType)
        {
            if (!IsAuthenticated || CurrentUser == null)
                return false;

            UpdateSessionActivity();
            return CurrentUser.UserType == requiredUserType || CurrentUser.UserType == "Admin";
        }

        /// <summary>
        /// Checks if the current user can access data for a specific user
        /// </summary>
        /// <param name="userId">The user ID to check access for</param>
        /// <returns>True if access is allowed, false otherwise</returns>
        public async Task<bool> CanAccessUserDataAsync(Guid userId)
        {
            if (!IsAuthenticated || CurrentUser == null)
                return false;

            UpdateSessionActivity();

            // Admin can access all data
            if (CurrentUser.UserType == "Admin")
                return true;

            // Users can access their own data
            if (CurrentUser.UserId == userId)
                return true;

            // Clinicians can access their assigned patients' data
            if (CurrentUser.UserType == "Clinician")
            {
                try
                {
                    using var context = new SensoreDbContext();
                    var patient = await context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userId);
                    return patient?.AssignedClinicianId == CurrentUser.UserId;
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogError($"Error checking data access for user: {userId}", ex, "Auth");
                    return false;
                }
            }

            return false;
        }

        // Keep synchronous version for backward compatibility but mark as obsolete
        [Obsolete("Use CanAccessUserDataAsync instead for better performance")]
        public bool CanAccessUserData(Guid userId)
        {
            return CanAccessUserDataAsync(userId).GetAwaiter().GetResult();
        }
    }
}
