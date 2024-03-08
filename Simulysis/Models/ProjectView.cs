using System.ComponentModel.DataAnnotations;

namespace Simulysis.Models
{
    public class ProjectView
    {
        [Required(ErrorMessage = "Project name required.")]
        public string ProjectName { get; set; }

        public string Description { get; set; }

        [Display(Name = "Project file")]
        [Required(ErrorMessage = "Project file required.")]
        public IFormFile File { get; set; }

        [Display(Name = "Original project")]
        public long OriginalProjectId { get; set; }

        [Display(Name = "Safe Uploading (slow) ")]
        public bool safeUpload { get; set; } = true;

        public List<ExistingProjectInfo> ExistingProjectInfos = new();
    }
}