using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using SharedKernel;

namespace Application.Common.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(message: "Procesando solicitud {RequestName}", requestName);

        var result = await next();

        if (result.IsSuccess)
        {
            logger.LogInformation(message: "Solicitud completada {RequestName}", requestName);
        }
        else
        {
            using (LogContext.PushProperty(name: "Error", result.Error, destructureObjects: true))
            {
                logger.LogError(message: "Solicitud completada {RequestName} con error", requestName);
            }
        }

        return result;
    }
}