namespace AssistantHub.McpServer.Classes
{
    using System;
    using System.Net;

    /// <summary>
    /// TCP server settings.
    /// </summary>
    public class TcpServerSettings
    {
        private string _Address = "127.0.0.1";
        private int _Port = 8821;

        /// <summary>
        /// Address on which to listen.
        /// </summary>
        public string Address
        {
            get => _Address;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(Address));
                if (!value.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                    IPAddress.Parse(value).ToString();
                _Address = value;
            }
        }

        /// <summary>
        /// TCP port.
        /// </summary>
        public int Port
        {
            get => _Port;
            set
            {
                if (value < 0 || value > 65535)
                    throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }
    }
}
