namespace wootware_devops_ftp_deployment;

public class Utilities
{
    public static string UrlCombine(string url1, string url2)
    {
        if (url1.Length == 0) {
            return url2;
        }

        if (url2.Length == 0) {
            return url1;
        }

        url1 = url1.TrimEnd('/', '\\');
        url2 = url2.TrimStart('/', '\\');

        return $"{url1}/{url2}";
    }
}