﻿using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Shared.Filters
{
    public class HttpResponseExceptionFilter : IActionFilter, IOrderedFilter
    {
        public int Order { get; set; } = int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context) {}

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                var response = new ApiResponse<object>(
                    statusCode:   500, 
                    message: context.Exception.Message,
                    stackTrace: context.Exception.StackTrace
                    );
                context.Result = new ObjectResult(response){ StatusCode = 500};
                context.ExceptionHandled = true;
            }
            if (context.Result == null || string.IsNullOrWhiteSpace(context.Result.ToString()))
                throw new System.Exception("Result value is null");
        }
    }
}
