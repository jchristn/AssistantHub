namespace AssistantHub.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Sdk.Models;

    /// <summary>
    /// Client for the AssistantHub API providing methods for assistants, collections, threads, chat, endpoints, documents, ingestion rules, and search.
    /// </summary>
    public class AssistantHubClient : AssistantHubClientManagementBase
    {
        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the AssistantHub client.
        /// </summary>
        /// <param name="baseUrl">Base URL of the AssistantHub server.</param>
        /// <param name="apiKey">Optional API key for authentication.</param>
        public AssistantHubClient(string baseUrl, string apiKey = null)
            : base(baseUrl, apiKey)
        {
        }

        /// <summary>
        /// Instantiate the AssistantHub client with a provided HttpClient.
        /// </summary>
        /// <param name="baseUrl">Base URL of the AssistantHub server.</param>
        /// <param name="httpClient">HttpClient instance to use.</param>
        /// <param name="apiKey">Optional API key for authentication.</param>
        public AssistantHubClient(string baseUrl, HttpClient httpClient, string apiKey = null)
            : base(baseUrl, httpClient, apiKey)
        {
        }

        #endregion
    }
}