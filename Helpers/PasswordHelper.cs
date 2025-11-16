using System;

namespace GrapheneSensore.Helpers
{
    /// <summary>
    /// Helper class for password hashing and validation
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Hash a password using BCrypt with a consistent work factor
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            
            // Use work factor of 11 for consistent performance
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(11));
        }

        /// <summary>
        /// Verify a password against a BCrypt hash
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Generate a cryptographically secure random password
        /// </summary>
        public static string GenerateRandomPassword(int length = 16)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            var chars = new char[length];
            var randomBytes = new byte[length];
            
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            
            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[randomBytes[i] % validChars.Length];
            }
            
            return new string(chars);
        }

        /// <summary>
        /// Validate password meets security requirements
        /// </summary>
        public static (bool isValid, string message) ValidatePassword(string password, int minLength = 8)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password cannot be empty");

            if (password.Length < minLength)
                return (false, $"Password must be at least {minLength} characters long");

            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;
            bool hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else if (!char.IsLetterOrDigit(c)) hasSpecial = true;
            }

            if (!hasUpper)
                return (false, "Password must contain at least one uppercase letter");
            if (!hasLower)
                return (false, "Password must contain at least one lowercase letter");
            if (!hasDigit)
                return (false, "Password must contain at least one digit");
            if (!hasSpecial)
                return (false, "Password must contain at least one special character");

            return (true, "Password is valid");
        }
    }
}
