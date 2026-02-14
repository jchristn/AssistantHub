namespace AssistantHub.Core.Settings
{
    using System;

    /// <summary>
    /// Webserver settings.
    /// </summary>
    public class WebserverSettings
    {
        #region Public-Members

        /// <summary>
        /// Hostname on which to listen.
        /// </summary>
        public string Hostname
        {
            get => _Hostname;
            set { if (!String.IsNullOrEmpty(value)) _Hostname = value; }
        }

        /// <summary>
        /// Port on which to listen.
        /// </summary>
        public int Port
        {
            get => _Port;
            set => _Port = (value >= 0 && value <= 65535) ? value : throw new ArgumentOutOfRangeException(nameof(Port));
        }

        /// <summary>
        /// Enable or disable SSL.
        /// </summary>
        public bool Ssl { get; set; } = false;

        #endregion

        #region Private-Members

        private string _Hostname = "localhost";
        private int _Port = 8800;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public WebserverSettings()
        {
        }

        #endregion
    }
}
