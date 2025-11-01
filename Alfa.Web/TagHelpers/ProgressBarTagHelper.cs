using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Alfa.Web.TagHelpers;

[HtmlTargetElement("progress-bar")]
public class ProgressBarTagHelper : TagHelper
{
    public int Value { get; set; } // 0..100
    public string? Label { get; set; }
    public string? Variant { get; set; }
    public bool Small { get; set; }
    public bool HideLabel { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var value = Math.Clamp(Value, 0, 100);
        var labelText = Label ?? $"{value}%";
        var variantClass = ResolveVariantClass(value);
        if (!string.IsNullOrWhiteSpace(Variant))
        {
            variantClass = $"bg-{Variant}";
        }

        var progressClasses = Small ? "progress progress-sm" : "progress";
        if (context.AllAttributes.TryGetAttribute("class", out var extraClass) && extraClass?.Value is string cls && !string.IsNullOrWhiteSpace(cls))
        {
            progressClasses = string.Join(" ", new[] { progressClasses, cls }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.RemoveAll("class");
        output.Attributes.SetAttribute("class", progressClasses);
        output.Attributes.SetAttribute("data-progressbar", "true");
        output.Attributes.SetAttribute("data-progress-value", value.ToString());
        if (HideLabel)
        {
            output.Attributes.SetAttribute("data-progress-hide-label", "true");
        }

        var inner = $"<div class=\"progress-bar {variantClass}\" role=\"progressbar\" style=\"width:{value}%;\" aria-valuenow=\"{value}\" aria-valuemin=\"0\" aria-valuemax=\"100\">" +
            (HideLabel ? $"<span class=\"visually-hidden\">{labelText}</span>" : labelText) +
            "</div>";

        output.Content.SetHtmlContent(inner);
    }

    private static string ResolveVariantClass(int value)
        => value >= 100 ? "bg-success" :
           value >= 70 ? "bg-primary" :
           value > 0 ? "bg-warning text-dark" :
           "bg-secondary";
}
