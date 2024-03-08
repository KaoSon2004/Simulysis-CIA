using System.ComponentModel.DataAnnotations;
using Entities.DTO;
using Entities.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Office.Interop.Excel;

namespace Simulysis.Models
{
    public class TableView
    {
        public IEnumerable<dynamic> Items { get; set; }
        public IEnumerable<ColumnProp> ColumnProps { get; set; }
        public int ItemPerPage { get; set; }
        public int CurrentPage { get; set; }
        public IEnumerable<int> PageSizes { get; set; }
        public string LinkAction { get; set; }
        public string LinkController { get; set; }
        public string PropUseToDelete { get; set; }
        public string SearchContent { get; set; }

        public string Test { get; set; }

        public List<long> treeImpactSet { get; set; }

        [Display(Name = "Dig Depth")]
        [Required(ErrorMessage = "Dig depth required")]
        public int digDepth { get; set; }

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

    public class DeleteView
    {
        public long IdToDelete { get; set; }

        public bool IsSelected { get; set; }

        public Dictionary<string, string> AdditionalInfo { get; set; }
    }
}