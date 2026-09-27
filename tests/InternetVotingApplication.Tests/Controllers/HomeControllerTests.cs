using InternetVotingApplication.Controllers;
using static InternetVotingApplication.Tests.Controllers.ControllerTestHelper;

namespace InternetVotingApplication.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void Static_pages_return_default_views()
    {
        var controller = new HomeController().Prepare();

        AssertView(controller.Index());
        AssertView(controller.Privacy());
        AssertView(controller.Contact());
    }

    [Fact]
    public void HttpStatus_sets_response_code_and_passes_it_to_the_view()
    {
        var controller = new HomeController().Prepare();

        var view = AssertView(controller.HttpStatus(404));

        Assert.Equal(404, controller.Response.StatusCode);
        Assert.Equal(404, view.Model);
    }

    [Fact]
    public void Error_exposes_request_id()
    {
        var controller = new HomeController().Prepare();
        controller.HttpContext.TraceIdentifier = "trace-1";

        AssertView(controller.Error());

        Assert.Equal("trace-1", controller.ViewBag.RequestId);
    }
}
