using System.ComponentModel.DataAnnotations;


namespace Simulysis.Models
{
    public class GitUpload
    {
        [Display(Name = "Project name")]
        [Required(ErrorMessage = "Project name required.")]
        public string ProjectName { get; set; }

        [Display(Name = "Project description")]
        public string Description { get; set; }

        [Display(Name = "Safe Uploading (slow) ")]
        public bool safeUpload { get; set; } = true;

        [Display(Name = "Enter git link")]
        [Required(ErrorMessage = "Git link required.")]
        public string GitLink { get; set; }

        [Display(Name = "Original project")]
        public long OriginalProjectId { get; set; }

        [Display(Name = "Branch")]
        public string Branch { get; set; }

        [Display(Name = "Commit")]
        public string Commit { get; set; }
    }
}
