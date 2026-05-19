using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using NexusAs.Domain.Exceptions;

namespace NexusAs.Infrastructure.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var response = new
            {
                Succeeded = false,
                Message = "Ocurrió un error inesperado.",
                Errors = new List<string>()
            };

            if (context.Exception is BusinessException businessEx)
            {
                response = new
                {
                    Succeeded = false,
                    Message = businessEx.Message,
                    Errors = new List<string>()
                };
                context.HttpContext.Response.StatusCode = 400;
            }
            else if (context.Exception is NotFoundException notFoundEx)
            {
                response = new
                {
                    Succeeded = false,
                    Message = notFoundEx.Message,
                    Errors = new List<string>()
                };
                context.HttpContext.Response.StatusCode = 404;
            }
            else
            {
                _logger.LogError(context.Exception,
                    "Error no controlado: {Message}", context.Exception.Message);
                context.HttpContext.Response.StatusCode = 500;
            }

            context.Result = new JsonResult(response);
            context.ExceptionHandled = true;
        }
    }
}