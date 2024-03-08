using Microsoft.AspNetCore.Mvc;

namespace Simulysis.Models
{
    public struct GitColumnItem
    {
        public string itemName;
        public string cssId;
        public string cssClass;
    }

    public class GitColumnView : Controller
    {
        public IEnumerable<GitColumnItem> Items { get; set; }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        public string ColumnName { get; set; }
        public string ControllerName { get; set; }
        public string CallName { get; set; }
    }
}
