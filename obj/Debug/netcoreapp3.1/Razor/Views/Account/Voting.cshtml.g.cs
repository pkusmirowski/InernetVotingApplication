#pragma checksum "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\Account\Voting.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "3d858df8fbee240a61699f52943b0f9392fdd305"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Account_Voting), @"mvc.1.0.view", @"/Views/Account/Voting.cshtml")]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#nullable restore
#line 1 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\_ViewImports.cshtml"
using InernetVotingApplication;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\_ViewImports.cshtml"
using InernetVotingApplication.Models;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"3d858df8fbee240a61699f52943b0f9392fdd305", @"/Views/Account/Voting.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"9eb89a762454365a413b17c3affc29d10d4d9a40", @"/Views/_ViewImports.cshtml")]
    public class Views_Account_Voting : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<InernetVotingApplication.ViewModels.KandydatViewModel>
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("asp-action", "VotingAdd", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("method", "post", global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        #pragma warning disable 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        #pragma warning restore 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 2 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\Account\Voting.cshtml"
  
    ViewData["Title"] = "Głosowanie";

#line default
#line hidden
#nullable disable
            WriteLiteral("<center>\r\n    <h2>Głosowanie</h2>\r\n    ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("form", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "3d858df8fbee240a61699f52943b0f9392fdd3054244", async() => {
                WriteLiteral("\r\n        <table border=\"1\">\r\n            <tr>\r\n                <th>Numer kandydata</th>\r\n                <th>Imię</th>\r\n                <th>Nazwisko</th>\r\n                <th>cos tam</th>\r\n            </tr>\r\n");
#nullable restore
#line 15 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\Account\Voting.cshtml"
             foreach (var election in Model.ElectionCandidates)
            {

#line default
#line hidden
#nullable disable
                WriteLiteral("        <tr>\r\n            <td>");
#nullable restore
#line 18 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\Account\Voting.cshtml"
           Write(election.Id);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\r\n            <td>");
#nullable restore
#line 19 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\Account\Voting.cshtml"
           Write(election.Imie);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\r\n            <td>");
#nullable restore
#line 20 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\Account\Voting.cshtml"
           Write(election.Nazwisko);

#line default
#line hidden
#nullable disable
                WriteLiteral("</td>\r\n            <td><input type=\"checkbox\" name=\"candidate\"");
                BeginWriteAttribute("value", " value=\"", 661, "\"", 681, 1);
#nullable restore
#line 21 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\Account\Voting.cshtml"
WriteAttributeValue("", 669, election.Id, 669, 12, false);

#line default
#line hidden
#nullable disable
                EndWriteAttribute();
                WriteLiteral(" onclick=\"MutExChkList(this);\" /></td>\r\n            <input type=\"hidden\" name=\"election\"");
                BeginWriteAttribute("value", " value=\"", 770, "\"", 789, 1);
#nullable restore
#line 22 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\Account\Voting.cshtml"
WriteAttributeValue("", 778, ViewBag.ID, 778, 11, false);

#line default
#line hidden
#nullable disable
                EndWriteAttribute();
                WriteLiteral(" />\r\n        </tr>\r\n");
#nullable restore
#line 24 "C:\Users\pawel\source\repos\InernetVotingApplication\InernetVotingApplication\Views\Account\Voting.cshtml"
        }

#line default
#line hidden
#nullable disable
                WriteLiteral("        </table>\r\n        <input type=\"submit\" value=\"submit\" />\r\n    ");
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper);
            __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper);
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper.Action = (string)__tagHelperAttribute_0.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_0);
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper.Method = (string)__tagHelperAttribute_1.Value;
            __tagHelperExecutionContext.AddTagHelperAttribute(__tagHelperAttribute_1);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral(@"
</center>

<script type=""text/javascript"">
    function MutExChkList(chk) {
        var chkList = chk.parentNode.parentNode.parentNode;
        var chks = chkList.getElementsByTagName(""input"");
        for (var i = 0; i < chks.length; i++) {
            if (chks[i] != chk && chk.checked) {
                chks[i].checked = false;
            }
        }
    }
</script>");
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<InernetVotingApplication.ViewModels.KandydatViewModel> Html { get; private set; }
    }
}
#pragma warning restore 1591
