using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    public class UserService
    {
        public async Task<List<User>> GetAllUsersAsync()
        {
            SensoreDbContext? context = null;
            try
            {
                System.Diagnostics.Debug.WriteLine("GetAllUsersAsync: Creating context...");
                context = new SensoreDbContext();
                
                System.Diagnostics.Debug.WriteLine("GetAllUsersAsync: Testing connection...");
                // Test connection first
                var canConnect = await context.Database.CanConnectAsync();
                System.Diagnostics.Debug.WriteLine($"GetAllUsersAsync: Can connect = {canConnect}");
                
                if (!canConnect)
                {
                    throw new InvalidOperationException("Cannot connect to database. Please check connection string and SQL Server status.");
                }
                
                System.Diagnostics.Debug.WriteLine("GetAllUsersAsync: Querying users...");
                // Try to get users with explicit null handling
                var users = await context.Users
                    .AsNoTracking()
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"GetAllUsersAsync: Retrieved {users?.Count ?? 0} users");
                
                if (users == null)
                {
                    System.Diagnostics.Debug.WriteLine("GetAllUsersAsync: Users list is null!");
                    return new List<User>();
                }
                
                System.Diagnostics.Debug.WriteLine("GetAllUsersAsync: Sorting users...");
                // If we got here, sort in memory to avoid SQL issues
                var sortedUsers = users.OrderBy(u => (u?.FirstName ?? u?.Username ?? "")).ToList();
                
                System.Diagnostics.Debug.WriteLine($"GetAllUsersAsync: Returning {sortedUsers.Count} sorted users");
                return sortedUsers;
            }
            catch (NullReferenceException nrEx)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllUsersAsync NullRef: {nrEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {nrEx.StackTrace}");
                throw new Exception($"Null reference in GetAllUsersAsync at: {nrEx.StackTrace?.Split('\n')[0] ?? "unknown"}\nMessage: {nrEx.Message}", nrEx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllUsersAsync Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                throw new Exception($"Error in GetAllUsersAsync: {ex.GetType().Name} - {ex.Message}\nInner: {ex.InnerException?.Message}", ex);
            }
            finally
            {
                context?.Dispose();
            }
        }

        public async Task<List<User>> GetUsersByTypeAsync(string userType)
        {
            try
            {
                using var context = new SensoreDbContext();
                var users = await context.Users
                    .AsNoTracking()
                    .Where(u => u.UserType == userType && u.IsActive)
                    .ToListAsync();
                    
                return users.OrderBy(u => u.FirstName ?? u.Username ?? "").ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetUsersByTypeAsync: {ex.Message}", ex);
            }
        }

        public async Task<List<User>> GetPatientsByClinicianAsync(Guid clinicianId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var users = await context.Users
                    .AsNoTracking()
                    .Where(u => u.AssignedClinicianId == clinicianId && u.IsActive)
                    .ToListAsync();
                    
                return users.OrderBy(u => u.FirstName ?? u.Username ?? "").ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetPatientsByClinicianAsync: {ex.Message}", ex);
            }
        }

        public async Task<(bool success, string message, User? user)> CreateUserAsync(
            string username, 
            string password, 
            string userType, 
            string firstName, 
            string lastName, 
            string email, 
            string? phoneNumber = null,
            Guid? assignedClinicianId = null)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username cannot be empty", null);
            
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password cannot be empty", null);
            
            if (string.IsNullOrWhiteSpace(userType))
                return (false, "User type cannot be empty", null);
            
            if (string.IsNullOrWhiteSpace(firstName))
                return (false, "First name cannot be empty", null);
            
            if (string.IsNullOrWhiteSpace(lastName))
                return (false, "Last name cannot be empty", null);
            
            try
            {
                using var context = new SensoreDbContext();
                
                // Check if username already exists
                if (await context.Users.AnyAsync(u => u.Username == username))
                {
                    return (false, "Username already exists", null);
                }

                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Username = username,
                    PasswordHash = PasswordHelper.HashPassword(password),
                    UserType = userType,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    AssignedClinicianId = assignedClinicianId,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                return (true, "User created successfully", user);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating user: {ex.Message}", null);
            }
        }

        public async Task<(bool success, string message)> UpdateUserAsync(User user)
        {
            try
            {
                using var context = new SensoreDbContext();
                var existingUser = await context.Users.FindAsync(user.UserId);
                
                if (existingUser == null)
                {
                    return (false, "User not found");
                }

                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.AssignedClinicianId = user.AssignedClinicianId;
                existingUser.IsActive = user.IsActive;

                await context.SaveChangesAsync();
                return (true, "User updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating user: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeleteUserAsync(Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var user = await context.Users.FindAsync(userId);
                
                if (user == null)
                {
                    return (false, "User not found");
                }

                // Soft delete - just deactivate
                user.IsActive = false;
                await context.SaveChangesAsync();

                return (true, "User deleted successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting user: {ex.Message}");
            }
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            using var context = new SensoreDbContext();
            return await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<List<User>> GetCliniciansAsync()
        {
            try
            {
                using var context = new SensoreDbContext();
                var users = await context.Users
                    .AsNoTracking()
                    .Where(u => u.UserType == "Clinician" && u.IsActive)
                    .ToListAsync();
                    
                return users.OrderBy(u => u.FirstName ?? u.Username ?? "").ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetCliniciansAsync: {ex.Message}", ex);
            }
        }

        public async Task<(bool success, string message)> ToggleUserActiveStatusAsync(Guid userId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var user = await context.Users.FindAsync(userId);
                
                if (user == null)
                {
                    return (false, "User not found");
                }

                user.IsActive = !user.IsActive;
                await context.SaveChangesAsync();

                return (true, $"User {(user.IsActive ? "activated" : "deactivated")} successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error toggling user status: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> AssignClinicianAsync(Guid patientId, Guid clinicianId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var patient = await context.Users.FindAsync(patientId);
                
                if (patient == null)
                {
                    return (false, "Patient not found");
                }

                if (patient.UserType != "Patient")
                {
                    return (false, "User is not a patient");
                }

                var clinician = await context.Users.FindAsync(clinicianId);
                if (clinician == null || clinician.UserType != "Clinician")
                {
                    return (false, "Invalid clinician");
                }

                patient.AssignedClinicianId = clinicianId;
                await context.SaveChangesAsync();

                return (true, "Clinician assigned successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error assigning clinician: {ex.Message}");
            }
        }
    }
}
