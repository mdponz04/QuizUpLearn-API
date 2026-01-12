using System.ComponentModel.DataAnnotations;

namespace BusinessLogic.Helpers
{
    public static class ValidateHelper
    {
        public static void Validate<T>(T dto)
        {
            var context = new ValidationContext(dto!);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(dto!, context, results, true))
            {
                var annotationMessages = string.Join(
                    ", ",
                    results.Select(r => r.ErrorMessage)
                );

                throw new ValidationException(
                    $"{annotationMessages}"
                );
            }
        }
    }
}
