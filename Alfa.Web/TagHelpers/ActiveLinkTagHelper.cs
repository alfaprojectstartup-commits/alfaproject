using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Alfa.Web.TagHelpers
{
    [HtmlTargetElement("a", Attributes = "asp-controller,asp-action")]
    public class ActiveLinkTagHelper : TagHelper
    {
        [ViewContext] public ViewContext? ViewContext { get; set; }
        public string? AspController { get; set; }
        public string? AspAction { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var vc = ViewContext!;
            var ctrl = vc.RouteData.Values["controller"]?.ToString();
            var act = vc.RouteData.Values["action"]?.ToString();

            if (string.Equals(ctrl, AspController, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(act, AspAction, StringComparison.OrdinalIgnoreCase))
            {
                var cls = output.Attributes.FirstOrDefault(a => a.Name == "class")?.Value?.ToString();
                output.Attributes.SetAttribute("class", (cls + " active").Trim());
            }
        }
    }
}
