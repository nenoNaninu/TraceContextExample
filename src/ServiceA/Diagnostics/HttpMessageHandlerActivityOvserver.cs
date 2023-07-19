using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ServiceA.Diagnostics;

class HttpMessageHandlerActivityOvserver : IObserver<DiagnosticListener>
{
    private readonly ILogger<HttpMessageHandlerActivityOvserver> _logger;

    public HttpMessageHandlerActivityOvserver(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<HttpMessageHandlerActivityOvserver>();
    }

    public void OnCompleted()
    {
        _logger.LogInformation("HttpMessageHandlerActivityOvserver.OnCompleted");
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error, "HttpMessageHandlerActivityOvserver.OnError");
    }

    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == "HttpHandlerDiagnosticListener")
        {
            listener.Subscribe(new CoreOvserver(_logger));
        }
    }

    private class CoreOvserver : IObserver<KeyValuePair<string, object?>>
    {
        private readonly ILogger<HttpMessageHandlerActivityOvserver> _logger;

        public CoreOvserver(ILogger<HttpMessageHandlerActivityOvserver> logger)
        {
            _logger = logger;
        }

        public void OnCompleted()
        {
            _logger.LogInformation("HttpMessageHandlerActivityOvserver.CoreOvserver.OnCompleted");
        }

        public void OnError(Exception error)
        {
            _logger.LogError(error, "HttpMessageHandlerActivityOvserver.CoreOvserver.OnError");
        }

        public void OnNext(KeyValuePair<string, object?> keyValuePair)
        {
            _logger.LogInformation("Received activity event from HttpMessageHandler. Event name is {EventName}.", keyValuePair.Key);
        }
    }
}
