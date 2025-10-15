using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Alfa.Web.TagHelpers;

[HtmlTargetElement("bs-form-field")]
public class BsFormFieldTagHelper : TagHelper
{
    [HtmlAttributeName("label")]
    public string? Label { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", "mb-3");
        var child = (output.GetChildContentAsync().GetAwaiter().GetResult()).GetContent();
        output.Content.SetHtmlContent($"""
            <label class="form-label">{Label}</label>
            {child}
        """);
    }
}
