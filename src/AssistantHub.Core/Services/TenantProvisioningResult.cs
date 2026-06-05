namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Result of tenant provisioning.
    /// </summary>
    public class TenantProvisioningResult
    {
        /// <summary>Tenant ID.</summary>
        public string TenantId { get; set; }

        /// <summary>Tenant name.</summary>
        public string TenantName { get; set; }

        /// <summary>Admin user ID.</summary>
        public string AdminUserId { get; set; }

        /// <summary>Admin email.</summary>
        public string AdminEmail { get; set; }

        /// <summary>Admin password (plaintext, for initial provisioning only).</summary>
        public string AdminPassword { get; set; }

        /// <summary>Default bearer token.</summary>
        public string BearerToken { get; set; }

        /// <summary>Admin user created during provisioning.</summary>
        public UserMaster User { get; set; }

        /// <summary>Credential created during provisioning.</summary>
        public Credential Credential { get; set; }
    }
}
