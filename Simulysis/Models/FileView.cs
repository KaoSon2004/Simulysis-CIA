using System.ComponentModel.DataAnnotations;

namespace Simulysis.Models
{
    public class NewFileView
    {
        [Display(Name = "File")]
        [Required(ErrorMessage = "File required.")]
        public IFormFile File { get; set; }

        [Display(Name = "File description")]
        public string Description { get; set; }

        public long ProjectId { get; set; }
    }

    public class EditFileView
    {
        [Display(Name = "File")]
        public IFormFile File { get; set; }

        public long FileId { get; set; }

        [Display(Name = "File description")]
        public string Description { get; set; }

        public long ProjectId { get; set; }
    }
}