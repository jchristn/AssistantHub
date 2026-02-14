namespace AssistantHub.Core.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Models;
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

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="database">Database driver.</param>
        /// <param name="logging">Logging module.</param>
        public AuthenticationService(DatabaseDriverBase database, LoggingModule logging)
        {
            _Database = database ?? throw new ArgumentNullException(nameof(database));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Authenticate using a bearer token.
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
                Credential credential = await _Database.Credential.ReadByBearerTokenAsync(bearerToken, token).ConfigureAwait(false);
                if (credential == null || !credential.Active)
                    return new AuthenticateResult { Success = false, ErrorMessage = "Invalid or inactive bearer token." };

                UserMaster user = await _Database.User.ReadAsync(credential.UserId, token).ConfigureAwait(false);
                if (user == null || !user.Active)
                    return new AuthenticateResult { Success = false, ErrorMessage = "User not found or inactive." };

                UserMaster redactedUser = new UserMaster
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsAdmin = user.IsAdmin,
                    Active = user.Active,
                    CreatedUtc = user.CreatedUtc,
                    LastUpdateUtc = user.LastUpdateUtc
                };

                return new AuthenticateResult
                {
                    Success = true,
                    User = redactedUser,
                    Credential = credential
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
        /// <param name="email">Email address.</param>
        /// <param name="password">Password.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authentication result.</returns>
        public async Task<AuthenticateResult> AuthenticateByEmailPasswordAsync(string email, string password, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(email) || String.IsNullOrEmpty(password))
                return new AuthenticateResult { Success = false, ErrorMessage = "Email and password are required." };

            try
            {
                UserMaster user = await _Database.User.ReadByEmailAsync(email, token).ConfigureAwait(false);
                if (user == null || !user.Active)
                    return new AuthenticateResult { Success = false, ErrorMessage = "Invalid email or password." };

                if (!user.VerifyPassword(password))
                    return new AuthenticateResult { Success = false, ErrorMessage = "Invalid email or password." };

                EnumerationQuery query = new EnumerationQuery { MaxResults = 1 };
                EnumerationResult<Credential> credentials = await _Database.Credential.EnumerateAsync(query, token).ConfigureAwait(false);

                Credential credential = null;
                if (credentials != null && credentials.Objects != null)
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

                UserMaster redactedUser = new UserMaster
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsAdmin = user.IsAdmin,
                    Active = user.Active,
                    CreatedUtc = user.CreatedUtc,
                    LastUpdateUtc = user.LastUpdateUtc
                };

                return new AuthenticateResult
                {
                    Success = true,
                    User = redactedUser,
                    Credential = credential
                };
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception during email/password authentication: " + e.Message);
                return new AuthenticateResult { Success = false, ErrorMessage = "Authentication failed." };
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
