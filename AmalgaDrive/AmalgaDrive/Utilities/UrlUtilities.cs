using System.Text;

namespace AmalgaDrive.Utilities
{
    internal static class UrlUtilities
    {
        public static string UrlCombine(params string[] urls)
        {
            if (urls == null)
                return null;

            var sb = new StringBuilder();
            foreach (var url in urls)
            {
                if (string.IsNullOrEmpty(url))
                    continue;

                if (sb.Length > 0)
                {
                    if (sb[sb.Length - 1] != '/' && url[0] != '/')
                    {
                        sb.Append('/');
                    }
                }
                sb.Append(url);
            }
            return sb.ToString();
        }
    }
}
