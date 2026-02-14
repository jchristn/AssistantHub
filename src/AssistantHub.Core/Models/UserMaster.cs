namespace AssistantHub.Core.Models
{
    using System;
    using System.Data;
    using System.Security.Cryptography;
    using System.Text;
    using AssistantHub.Core.Helpers;

    /// <summary>
    /// User master record.
    /// </summary>
    public class UserMaster
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier with prefix usr_.
        /// </summary>
        public string Id
        {
            get => _Id;
            set => _Id = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Id));
        }

        /// <summary>
        /// Email address.
        /// </summary>
        public string Email
        {
            get => _Email;
            set => _Email = !String.IsNullOrEmpty(value) ? value : throw new ArgumentNullException(nameof(Email));
        }

        /// <summary>
        /// SHA-256 hash of the password.
        /// </summary>
        public string PasswordSha256 { get; set; } = null;

        /// <summary>
        /// First name.
        /// </summary>
        public string FirstName { get; set; } = null;

        /// <summary>
        /// Last name.
        /// </summary>
        public string LastName { get; set; } = null;

        /// <summary>
        /// Indicates whether the user is an administrator.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Indicates whether the user is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Timestamp when the record was created in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the record was last updated in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.NewUserId();
        private string _Email = "user@example.com";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public UserMaster()
        {
        }

        /// <summary>
        /// Create a UserMaster from a DataRow.
        /// </summary>
        /// <param name="row">Data row.</param>
        /// <returns>UserMaster instance or null.</returns>
        public static UserMaster FromDataRow(DataRow row)
        {
            if (row == null) return null;
            UserMaster obj = new UserMaster();
            obj.Id = DataTableHelper.GetStringValue(row, "id");
            obj.Email = DataTableHelper.GetStringValue(row, "email");
            obj.PasswordSha256 = DataTableHelper.GetStringValue(row, "password_sha256");
            obj.FirstName = DataTableHelper.GetStringValue(row, "first_name");
            obj.LastName = DataTableHelper.GetStringValue(row, "last_name");
            obj.IsAdmin = DataTableHelper.GetBooleanValue(row, "is_admin");
            obj.Active = DataTableHelper.GetBooleanValue(row, "active", true);
            obj.CreatedUtc = DataTableHelper.GetDateTimeValue(row, "created_utc");
            obj.LastUpdateUtc = DataTableHelper.GetDateTimeValue(row, "last_update_utc");
            return obj;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Set the password by computing its SHA-256 hash.
        /// </summary>
        /// <param name="password">Plain-text password.</param>
        public void SetPassword(string password)
        {
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                PasswordSha256 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Verify a plain-text password against the stored hash.
        /// </summary>
        /// <param name="password">Plain-text password.</param>
        /// <returns>True if the password matches.</returns>
        public bool VerifyPassword(string password)
        {
            if (String.IsNullOrEmpty(password)) return false;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                string hashed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                return String.Equals(PasswordSha256, hashed, StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion
    }
}
