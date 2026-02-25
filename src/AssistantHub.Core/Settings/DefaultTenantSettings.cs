namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// Default tenant settings for first-run provisioning.
    /// </summary>
    public class DefaultTenantSettings
    {
        #region Public-Members

        /// <summary>
        /// Default tenant display name.
        /// </summary>
        public string Name { get; set; } = "Default Tenant";

        /// <summary>
        /// Default admin email address.
        /// </summary>
        public string AdminEmail { get; set; } = "admin@assistanthub";

        /// <summary>
        /// Default admin password.
        /// </summary>
        public string AdminPassword { get; set; } = "password";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public DefaultTenantSettings()
        {
        }

        #endregion
    }
}
