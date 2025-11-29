using GrapheneSensore.Configuration;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GrapheneSensore.Validation
{
    /// <summary>
    /// Validates user inputs and data integrity
    /// </summary>
    public static class InputValidator
    {
        /// <summary>
        /// Validates password strength based on configured requirements
        /// </summary>
        public static (bool isValid, string message) ValidatePassword(string password)
        {
            var config = AppConfiguration.Instance;

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password cannot be empty");

            if (password.Length < config.PasswordMinLength)
                return (false, $"Password must be at least {config.PasswordMinLength} characters long");

            if (config.RequireUppercase && !password.Any(char.IsUpper))
                return (false, "Password must contain at least one uppercase letter");

            if (config.RequireDigit && !password.Any(char.IsDigit))
                return (false, "Password must contain at least one digit");

            if (config.RequireSpecialChar && !Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
                return (false, "Password must contain at least one special character");

            return (true, "Password is valid");
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        public static (bool isValid, string message) ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Email cannot be empty");

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                if (!emailRegex.IsMatch(email))
                    return (false, "Invalid email format");

                return (true, "Email is valid");
            }
            catch
            {
                return (false, "Invalid email format");
            }
        }

        /// <summary>
        /// Validates username format
        /// </summary>
        public static (bool isValid, string message) ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username cannot be empty");

            if (username.Length < 3)
                return (false, "Username must be at least 3 characters long");

            if (username.Length > 50)
                return (false, "Username cannot exceed 50 characters");

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
                return (false, "Username can only contain letters, numbers, and underscores");

            return (true, "Username is valid");
        }

        /// <summary>
        /// Validates user name (first/last name)
        /// </summary>
        public static (bool isValid, string message) ValidateName(string name, string fieldName = "Name")
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, $"{fieldName} cannot be empty");

            if (name.Length < 2)
                return (false, $"{fieldName} must be at least 2 characters long");

            if (name.Length > 100)
                return (false, $"{fieldName} cannot exceed 100 characters");

            return (true, $"{fieldName} is valid");
        }

        /// <summary>
        /// Validates pressure matrix data
        /// </summary>
        public static (bool isValid, string message) ValidateMatrix(int[,]? matrix)
        {
            if (matrix == null)
                return (false, "Matrix data is null");

            var matrixSize = AppConfiguration.Instance.MatrixSize;

            if (matrix.GetLength(0) != matrixSize || matrix.GetLength(1) != matrixSize)
                return (false, $"Matrix must be {matrixSize}x{matrixSize}");

            // Check for valid pressure values (1-255)
            for (int i = 0; i < matrixSize; i++)
            {
                for (int j = 0; j < matrixSize; j++)
                {
                    if (matrix[i, j] < 0 || matrix[i, j] > 255)
                        return (false, $"Invalid pressure value at [{i},{j}]: {matrix[i, j]}. Must be between 0 and 255");
                }
            }

            return (true, "Matrix is valid");
        }

        /// <summary>
        /// Validates CSV file format
        /// </summary>
        public static (bool isValid, string message) ValidateCsvFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return (false, "File name cannot be empty");

            if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return (false, "File must be a CSV file");

            // Expected format: {userId}_{YYYYMMDD}.csv
            var fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
            var parts = fileNameWithoutExt.Split('_');

            if (parts.Length != 2)
                return (false, "File name must follow format: {userId}_{YYYYMMDD}.csv");

            if (!Guid.TryParse(parts[0], out _) && parts[0].Length != 8) // Allow 8-char hex IDs
                return (false, "Invalid user ID in file name");

            if (!DateTime.TryParseExact(parts[1], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _))
                return (false, "Invalid date format in file name. Expected: YYYYMMDD");

            return (true, "File name is valid");
        }

        /// <summary>
        /// Validates date range
        /// </summary>
        public static (bool isValid, string message) ValidateDateRange(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
                return (false, "Both start and end dates must be provided");

            if (startDate.Value > endDate.Value)
                return (false, "Start date cannot be after end date");

            if (endDate.Value > DateTime.Now)
                return (false, "End date cannot be in the future");

            var daysDiff = (endDate.Value - startDate.Value).TotalDays;
            if (daysDiff > 365)
                return (false, "Date range cannot exceed 365 days");

            return (true, "Date range is valid");
        }

        /// <summary>
        /// Sanitizes user input to prevent injection attacks
        /// </summary>
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove potentially dangerous characters
            return Regex.Replace(input, @"[<>""';\\]", string.Empty).Trim();
        }
    }
}
