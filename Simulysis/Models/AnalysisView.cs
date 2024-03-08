using Entities.Types;
using System.ComponentModel.DataAnnotations;
using Entities.DTO;


namespace Simulysis.Models
{
    public class AnalysisView
    {
        [Display(Name = "New Version")]
        [Required(ErrorMessage = "New version required.")]
        public int NewVersion { get; set; }

        [Display(Name = "Old Version")]
        [Required(ErrorMessage = "Old version required.")]
        public int OldVersion { get; set; }

        public IEnumerable<dynamic> Items { get; set; }
        public IEnumerable<ColumnProp> ColumnProps { get; set; }
        public int ItemPerPage { get; set; }
        public int CurrentPage { get; set; }
        public IEnumerable<int> PageSizes { get; set; }
        public string LinkAction { get; set; }
        public string LinkController { get; set; }
        public string PropUseToDelete { get; set; }
        public string SearchContent { get; set; }

        public int PageCount()
        {
            return Convert.ToInt32(Math.Ceiling(Items.Count() / (double)ItemPerPage));
        }

        public List<dynamic> PaginatedItems()
        {
            int start = (CurrentPage - 1) * ItemPerPage;
            return Items.Skip(start).Take(ItemPerPage).ToList();
        }

        public List<DeleteView> DeleteViews { get; set; }
    }
}
