using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Core
{
    /// <summary>
    /// Basic
    /// </summary>
    public partial class QueueOrchestrator : IDisposable
    {
        bool _disposed = false;

        #region IDispose implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    
                }
                _disposed = true;
            }
        }

        #endregion
    }
}