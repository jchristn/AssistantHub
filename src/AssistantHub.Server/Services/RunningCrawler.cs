#pragma warning disable CS8625, CS8603, CS8600

namespace AssistantHub.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AssistantHub.Core;
    using AssistantHub.Core.Database;
    using AssistantHub.Core.Enums;
    using AssistantHub.Core.Helpers;
    using AssistantHub.Core.Models;
    using AssistantHub.Core.Services;
    using AssistantHub.Core.Services.Crawlers;
    using AssistantHub.Core.Settings;
    using SyslogLogging;

    internal class RunningCrawler
    {
        public CrawlerBase Crawler { get; set; } = null;
        public CancellationTokenSource Cancellation { get; set; } = null;
    }
}
