using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Helpers
{
    public class ValidateDtoHelper
    {
        public ValidateDtoHelper()
        {
        }

        public void ValidateDto(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
            {
                var errors = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
                throw new ValidationException($"Validation failed: {errors}");
            }
        }
    }
}
