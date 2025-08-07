using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SerenityHavenResort.ErrorHandling
{
    public class GlobalExceptionHandler : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            context.Result = new ObjectResult(new
            {
                Message = "An error occurred.",
                Exception = context.Exception.Message
            })
            {
                StatusCode = 500
            };
        }
    }
}
