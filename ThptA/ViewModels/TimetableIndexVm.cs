
using Microsoft.AspNetCore.Mvc.Rendering;
using ThptA.Models;

namespace ThptA.ViewModels
{
    public class TimetableIndexVm
    {
        public int? SchoolClassId { get; set; }

        public string ClassDisplay { get; set; } = "";

        public List<SelectListItem> ClassOptions { get; set; }
            = new();

        public List<TimetableEntry> Entries { get; set; }
            = new();
    }
}
