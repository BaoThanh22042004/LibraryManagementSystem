using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Web.Extensions
{
    /// <summary>
    /// Extension methods for working with FluentValidation results in MVC
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Adds FluentValidation errors to the MVC ModelState
        /// </summary>
        public static void AddToModelState(this FluentValidation.Results.ValidationResult result, ModelStateDictionary modelState)
        {
            foreach (var error in result.Errors)
            {
                modelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }
    }
}