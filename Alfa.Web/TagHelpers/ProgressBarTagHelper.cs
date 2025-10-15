using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Alfa.Web.TagHelpers;

[HtmlTargetElement("progress-bar")]
public class ProgressBarTagHelper : TagHelper
{
    public int Value { get; set; } // 0..100

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", "progress");
        output.Content.SetHtmlContent($"""
            <div class="progress-bar" role="progressbar" style="width:{Value}%;" aria-valuenow="{Value}" aria-valuemin="0" aria-valuemax="100">{Value}%</div>
        """);
    }
}
