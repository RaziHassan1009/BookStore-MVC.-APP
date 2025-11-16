namespace GrapheneSensore.Constants
{
    /// <summary>
    /// Application-wide constants for magic numbers and commonly used values
    /// </summary>
    public static class AppConstants
    {
        // Alert Severity Thresholds
        public const int CRITICAL_PRESSURE_THRESHOLD = 200;
        public const int HIGH_PRESSURE_THRESHOLD = 150;
        public const int MEDIUM_PRESSURE_THRESHOLD = 100;
        
        // User Roles
        public const string ROLE_ADMIN = "Admin";
        public const string ROLE_CLINICIAN = "Clinician";
        public const string ROLE_PATIENT = "Patient";
        
        // Alert Types
        public const string ALERT_TYPE_HIGH_PRESSURE = "HighPressure";
        public const string ALERT_TYPE_EXTENDED_PRESSURE = "ExtendedPressure";
        public const string ALERT_TYPE_LOW_CONTACT_AREA = "LowContactArea";
        public const string ALERT_TYPE_PRESSURE_SPIKE = "PressureSpike";
        
        // Severity Levels
        public const string SEVERITY_CRITICAL = "Critical";
        public const string SEVERITY_HIGH = "High";
        public const string SEVERITY_MEDIUM = "Medium";
        public const string SEVERITY_LOW = "Low";
        
        // Contact Area Thresholds
        public const decimal LOW_CONTACT_AREA_THRESHOLD = 50.0m;
        public const decimal OPTIMAL_CONTACT_AREA_MIN = 60.0m;
        public const decimal OPTIMAL_CONTACT_AREA_MAX = 80.0m;
        
        // Report Types
        public const string REPORT_TYPE_DAILY = "Daily";
        public const string REPORT_TYPE_WEEKLY = "Weekly";
        public const string REPORT_TYPE_HOURLY = "Hourly";
        public const string REPORT_TYPE_COMPARISON = "Comparison";
        public const string REPORT_TYPE_CUSTOM = "Custom";
        
        // CSV Import
        public const int FRAME_INTERVAL_SECONDS = 2;
        
        // UI Display
        public const int MAX_RECENT_ALERTS_DISPLAY = 10;
        public const int MAX_RECENT_COMMENTS_DISPLAY = 20;
        
        // Pagination
        public const int DEFAULT_PAGE_SIZE = 50;
        public const int MAX_PAGE_SIZE = 200;
        
        // File Formats
        public const string CSV_FILE_EXTENSION = ".csv";
        public const string CSV_FILENAME_PATTERN = @"^[a-fA-F0-9]{8}_\d{8}\.csv$";
        
        // Date Formats
        public const string DATE_FORMAT_FILENAME = "yyyyMMdd";
        public const string DATE_FORMAT_DISPLAY = "yyyy-MM-dd";
        public const string DATETIME_FORMAT_DISPLAY = "yyyy-MM-dd HH:mm:ss";
        
        // Error Messages
        public const string ERROR_INVALID_CREDENTIALS = "Invalid username or password";
        public const string ERROR_ACCOUNT_DISABLED = "This account has been disabled";
        public const string ERROR_SESSION_EXPIRED = "Your session has expired. Please login again";
        public const string ERROR_INSUFFICIENT_PERMISSIONS = "You do not have permission to perform this action";
        public const string ERROR_DATABASE_CONNECTION = "Unable to connect to database";
        public const string ERROR_FILE_NOT_FOUND = "The specified file was not found";
        public const string ERROR_INVALID_FILE_FORMAT = "Invalid file format";
        
        // Success Messages
        public const string SUCCESS_LOGIN = "Login successful";
        public const string SUCCESS_USER_CREATED = "User created successfully";
        public const string SUCCESS_USER_UPDATED = "User updated successfully";
        public const string SUCCESS_PASSWORD_CHANGED = "Password changed successfully";
        public const string SUCCESS_DATA_IMPORTED = "Data imported successfully";
        public const string SUCCESS_ALERT_ACKNOWLEDGED = "Alert acknowledged";
        public const string SUCCESS_COMMENT_ADDED = "Comment added successfully";
    }
}
