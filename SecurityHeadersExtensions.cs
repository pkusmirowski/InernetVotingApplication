namespace InternetVotingApplication
{
    public static class SecurityHeadersExtensions
    {
        /// <summary>
        /// Adds a conservative set of security headers to every response.
        /// </summary>
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                headers["Content-Security-Policy"] =
                    "default-src 'self'; " +
                    "img-src 'self' data:; " +
                    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                    "font-src 'self' https://fonts.gstatic.com; " +
                    "script-src 'self'; " +
                    "form-action 'self'; " +
                    "frame-ancestors 'none'";
                await next();
            });
        }
    }
}
