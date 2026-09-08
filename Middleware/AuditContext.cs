namespace EnglishCenter.API.Middleware
{
    public static class AuditContext
    {
        public static int? GetUserId(HttpContext context)
        {
            return context.Items["AuditUserId"] as int?;
        }

        public static string? GetIPAddress(HttpContext context)
        {
            return context.Items["AuditIPAddress"] as string;
        }
    }
}