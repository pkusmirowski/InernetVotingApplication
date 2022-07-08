using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;

namespace InternetVotingApplicationTests
{
    public static class SessionManager
    {
        public static void EmptySession(Controller currentController)
        {
            Mock<ISession> sessionMock = new();
            currentController.ControllerContext.HttpContext = new DefaultHttpContext
            {
                Session = sessionMock.Object
            };
        }

        public static void NotEmptySession(Controller currentController)
        {
            var mockContext = new Mock<HttpContext>();
            var mockSession = new Mock<ISession>();
            const string sessionValue = "email";
            byte[] dummy = Encoding.UTF8.GetBytes(sessionValue);
            mockSession.Setup(x => x.TryGetValue(It.IsAny<string>(), out dummy)).Returns(true);
            mockContext.Setup(s => s.Session).Returns(mockSession.Object);
            currentController.ControllerContext.HttpContext = mockContext.Object;
        }
    }
}
