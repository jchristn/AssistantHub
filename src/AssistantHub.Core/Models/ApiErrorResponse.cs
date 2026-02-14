namespace AssistantHub.Core.Models
{
    using System;
    using System.Text.Json.Serialization;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// API error response returned to clients.
    /// </summary>
    public class ApiErrorResponse
    {
        #region Public-Members

        /// <summary>
        /// Error type.
        /// </summary>
        public ApiErrorEnum Error { get; set; } = ApiErrorEnum.InternalError;

        /// <summary>
        /// Human-readable error message derived from the error type.
        /// </summary>
        public string Message
        {
            get
            {
                switch (Error)
                {
                    case ApiErrorEnum.AuthenticationFailed:
                        return "Authentication failed. Please check your credentials.";
                    case ApiErrorEnum.AuthorizationFailed:
                        return "Authorization failed. You do not have permission to perform this action.";
                    case ApiErrorEnum.BadRequest:
                        return "Bad request. Please check your request and try again.";
                    case ApiErrorEnum.NotFound:
                        return "The requested resource was not found.";
                    case ApiErrorEnum.Conflict:
                        return "A conflict occurred. The resource already exists or has been modified.";
                    case ApiErrorEnum.InternalError:
                    default:
                        return "An internal error occurred. Please try again later.";
                }
            }
        }

        /// <summary>
        /// HTTP status code derived from the error type.
        /// </summary>
        public int StatusCode
        {
            get
            {
                switch (Error)
                {
                    case ApiErrorEnum.AuthenticationFailed:
                        return 401;
                    case ApiErrorEnum.AuthorizationFailed:
                        return 403;
                    case ApiErrorEnum.BadRequest:
                        return 400;
                    case ApiErrorEnum.NotFound:
                        return 404;
                    case ApiErrorEnum.Conflict:
                        return 409;
                    case ApiErrorEnum.InternalError:
                    default:
                        return 500;
                }
            }
        }

        /// <summary>
        /// Optional context object providing additional error details.
        /// </summary>
        public object Context { get; set; } = null;

        /// <summary>
        /// Optional description providing additional error information.
        /// </summary>
        public string Description { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ApiErrorResponse()
        {
        }

        /// <summary>
        /// Instantiate with an error type and optional context and description.
        /// </summary>
        /// <param name="error">API error type.</param>
        /// <param name="context">Optional context object.</param>
        /// <param name="description">Optional description.</param>
        public ApiErrorResponse(ApiErrorEnum error, object context = null, string description = null)
        {
            Error = error;
            Context = context;
            Description = description;
        }

        #endregion
    }
}
