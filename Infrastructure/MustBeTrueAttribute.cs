using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace VertexLearn.Infrastructure
{
    /// <summary>
    /// Validates that a checkbox (bool property) is ticked, on both the server and the client.
    /// Replaces [Range(typeof(bool), "true", "true")], which jQuery Validate cannot evaluate
    /// (it converts "true" to a number, gets NaN, and so the check always fails in the browser).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
    {
        public MustBeTrueAttribute() : base("This box must be ticked.") { }

        public override bool IsValid(object? value) => value is bool b && b;

        // Emits data-val-mustbetrue="..." on the <input>; the matching JS rule is in _ValidationScriptsPartial
        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes["data-val"] = "true";
            context.Attributes["data-val-mustbetrue"] = FormatErrorMessage(context.ModelMetadata.GetDisplayName());
        }
    }
}
