using net.r_eg.MvsSln.Core;

namespace dotdock
{
    public static class Extensions
    {
        public static string ToPath(this ProjectItem self)
        {
            return self.path.Replace("\\", "/");
        }
    }
}