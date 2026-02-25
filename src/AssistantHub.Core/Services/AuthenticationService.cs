namespace AssistantHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Authentication service.
    /// </summary>
    public class AuthenticationService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[AuthenticationService] ";
        private DatabaseDriverBase _Database = null;
        private LoggingModule _Logging = null;
        private AssistantHubSettings _Settings = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="settings">Application settings.</param>
        public AuthenticationService(DatabaseDriverBase database, LoggingModule logging, AssistantHubSettings settings = null)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Settings = settings;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Authenticate a bearer token and return an AuthContext.
        /// Checks admin API keys first, then credential-based auth with tenant validation.
        /// </summary>
        /// <param name="bearerToken">Bearer token.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>AuthContext.</returns>
        public async Task<AuthContext> AuthenticateBearerAsync(string bearerToken, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bearerToken))
                return null;

            try
            {
                // Check if token is a global admin API key
                if (_Settings?.AdminApiKeys != null && _Settings.AdminApiKeys.Contains(bearerToken))
                {
                    return new AuthContext
                    {
                        IsAuthenticated = true,
                        IsGlobalAdmin = true,
                        IsTenantAdmin = false,
                        TenantId = null,
                        UserId = null,
                        CredentialId = null,
                        Email = null,
                        Tenant = null,
                        User = null
                    };
                }

                // Look up credential by bearer token
                Credential credential = await _Database.Credential.ReadByBearerTokenAsync(bearerToken, token).ConfigureAwait(false);
                if (credential == null || !credential.Active)
                    return null;

                // Load user
                UserMaster user = await _Database.User.ReadAsync(credential.UserId, token).ConfigureAwait(false);
                if (user == null || !user.Active)
                    return null;

                // Load tenant
                TenantMetadata tenant = await _Database.Tenant.ReadByIdAsync(credential.TenantId, token).ConfigureAwait(false);
                if (tenant == null || !tenant.Active)
                    return null;

                return new AuthContext
                {
                    IsAuthenticated = true,
                    IsGlobalAdmin = user.IsAdmin,
                    IsTenantAdmin = user.IsTenantAdmin,
                    TenantId = tenant.Id,
                    UserId = user.Id,
                    CredentialId = credential.Id,
                    Email = user.Email,
                    Tenant = tenant,
                    User = user
                };
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during bearer authentication: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Authenticate using a bearer token (legacy - returns AuthenticateResult).
        /// </summary>
        /// <param name="bearerToken">Bearer token.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authentication result.</returns>
        public async Task<AuthenticateResult> AuthenticateByBearerTokenAsync(string bearerToken, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bearerToken))
                return new AuthenticateResult { Success = false, ErrorMessage = "Bearer token is required." };

            try
            {
                AuthContext auth = await AuthenticateBearerAsync(bearerToken, token).ConfigureAwait(false);
                if (auth == null)
                    return new AuthenticateResult { Success = false, ErrorMessage = "Invalid or inactive bearer token." };

                // API-key-based global admin (no user record) — synthesize user and credential
                if (auth.User == null)
                {
                    return new AuthenticateResult
                    {
                        Success = true,
                        User = new UserMaster
                        {
                            Id = "admin",
                            TenantId = Constants.DefaultTenantId,
                            Email = "admin",
                            FirstName = "Global",
                            LastName = "Admin",
                            IsAdmin = true,
                            IsTenantAdmin = true,
                            Active = true
                        },
                        Credential = new Credential
                        {
                            Id = "admin",
                            TenantId = Constants.DefaultTenantId,
                            UserId = "admin",
                            Name = "Admin API Key",
                            BearerToken = bearerToken
                        },
                        TenantId = Constants.DefaultTenantId,
                        TenantName = Constants.DefaultTenantName,
                        IsGlobalAdmin = true,
                        IsTenantAdmin = true
                    };
                }

                UserMaster redactedUser = RedactUser(auth.User);

                return new AuthenticateResult
                {
                    Success = true,
                    User = redactedUser,
                    Credential = new Credential
                    {
                        Id = auth.CredentialId,
                        TenantId = auth.TenantId,
                        UserId = auth.UserId,
                        BearerToken = bearerToken
                    },
                    TenantId = auth.TenantId,
                    TenantName = auth.Tenant?.Name,
                    IsGlobalAdmin = auth.IsGlobalAdmin,
                    IsTenantAdmin = auth.IsTenantAdmin
                };
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during bearer token authentication: " + e.Message);
                return new AuthenticateResult { Success = false, ErrorMessage = "Authentication failed." };
            }
        }

        /// <summary>
        /// Authenticate using email and password.
        /// </summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="email">Email address.</param>
        /// <param name="password">Password.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authentication result.</returns>
        public async Task<AuthenticateResult> AuthenticateByEmailPasswordAsync(string tenantId, string email, string password, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(email) || String.IsNullOrEmpty(password))
                return new AuthenticateResult { Success = false, ErrorMessage = "Email and password are required." };

            // Default to "default" tenant if not specified
            if (String.IsNullOrEmpty(tenantId)) tenantId = Constants.DefaultTenantId;

            try
            {
                // Validate tenant
                TenantMetadata tenant = await _Database.Tenant.ReadByIdAsync(tenantId, token).ConfigureAwait(false);
                if (tenant == null || !tenant.Active)
                    return new AuthenticateResult { Success = false, ErrorMessage = "Tenant not found or inactive." };

                UserMaster user = await _Database.User.ReadByEmailAsync(tenantId, email, token).ConfigureAwait(false);
                if (user == null || !user.Active)
                    return new AuthenticateResult { Success = false, ErrorMessage = "Invalid email or password." };

                if (!user.VerifyPassword(password))
                    return new AuthenticateResult { Success = false, ErrorMessage = "Invalid email or password." };

                // Find an active credential for this user
                EnumerationQuery query = new EnumerationQuery { MaxResults = 100 };
                EnumerationResult<Credential> credentials = await _Database.Credential.EnumerateAsync(tenantId, query, token).ConfigureAwait(false);

                Credential credential = null;
                if (credentials?.Objects != null)
                {
                    foreach (Credential cred in credentials.Objects)
                    {
                        if (cred.UserId == user.Id && cred.Active)
                        {
                            credential = cred;
                            break;
                        }
                    }
                }

                UserMaster redactedUser = RedactUser(user);

                return new AuthenticateResult
                {
                    Success = true,
                    User = redactedUser,
                    Credential = credential,
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    IsGlobalAdmin = user.IsAdmin,
                    IsTenantAdmin = user.IsTenantAdmin
                };
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during email/password authentication: " + e.Message);
                return new AuthenticateResult { Success = false, ErrorMessage = "Authentication failed." };
            }
        }

        /// <summary>
        /// Authenticate using email and password (legacy overload without tenantId).
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="password">Password.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authentication result.</returns>
        public Task<AuthenticateResult> AuthenticateByEmailPasswordAsync(string email, string password, CancellationToken token = default)
        {
            return AuthenticateByEmailPasswordAsync(Constants.DefaultTenantId, email, password, token);
        }

        #endregion

        #region Private-Methods

        private UserMaster RedactUser(UserMaster user)
        {
            return new UserMaster
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsAdmin = user.IsAdmin,
                IsTenantAdmin = user.IsTenantAdmin,
                Active = user.Active,
                CreatedUtc = user.CreatedUtc,
                LastUpdateUtc = user.LastUpdateUtc
            };
        }

        #endregion
    }
}
