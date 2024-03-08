using System.ComponentModel.DataAnnotations;

namespace Simulysis.Models
{
    public class LogConfigView
    {
        [Display(Name = "Log level")]
        [Required(ErrorMessage = "Log level is required")]
        public string SelectedLogLevel { get; set; }

        public Microsoft.AspNetCore.Mvc.Rendering.SelectList LogLevels { get; set; }
    }
}