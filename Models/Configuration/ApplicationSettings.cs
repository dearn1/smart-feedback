using Microsoft.AspNetCore.Mvc.Rendering;

namespace smart_feedback.Models.Configuration
{
    public class ApplicationSettings
    {
        public List<ProgrammeOption> Programmes { get; set; } = new();

        public List<SelectListItem> GetProgrammeSelectList(string selectedValue = null)
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Programmes", Selected = string.IsNullOrEmpty(selectedValue) }
            };

            items.AddRange(Programmes.Select(p => new SelectListItem
            {
                Value = p.Value,
                Text = p.Text,
                Selected = selectedValue == p.Value
            }));

            return items;
        }
    }

    public class ProgrammeOption
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
