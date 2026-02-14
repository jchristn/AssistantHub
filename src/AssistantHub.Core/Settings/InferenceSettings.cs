namespace AssistantHub.Core.Settings
{
    using System;
    using AssistantHub.Core.Enums;

    /// <summary>
    /// Inference service settings.
    /// </summary>
    public class InferenceSettings
    {
        #region Public-Members

        /// <summary>
        /// Inference provider type.
        /// </summary>
        public InferenceProviderEnum Provider { get; set; } = InferenceProviderEnum.Ollama;

        /// <summary>
        /// Endpoint URL for the inference provider.
        /// </summary>
        public string Endpoint
        {
            get => _Endpoint;
            set { if (!String.IsNullOrEmpty(value)) _Endpoint = value; }
        }

        /// <summary>
        /// API key for the inference provider.
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Default model to use for inference.
        /// </summary>
        public string DefaultModel
        {
            get => _DefaultModel;
            set { if (!String.IsNullOrEmpty(value)) _DefaultModel = value; }
        }

        #endregion

        #region Private-Members

        private string _Endpoint = "http://localhost:11434";
        private string _DefaultModel = "gemma3:4b";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public InferenceSettings()
        {
        }

        #endregion
    }
}
