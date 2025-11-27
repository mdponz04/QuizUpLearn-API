using System.ComponentModel.DataAnnotations;

namespace Repository.Entities
{
    public class AppSetting
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
