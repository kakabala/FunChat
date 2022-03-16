using System;
using System.Text.RegularExpressions;

namespace FunChat.Common
{
    public static class Helpers
    {
        public static bool IsValidUserName(string username)
        {
            string pattern = @"^\w{3,10}$";
            Regex rgx = new Regex(pattern);
            return rgx.IsMatch(username);
        }

        public static bool IsValidSecure(string secure)
        {
            string pattern = @"^\w{6,18}$";
            Regex rgx = new Regex(pattern);
            return rgx.IsMatch(secure);
        }

        public static bool IsAdmin(string username)
        {
            return string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetUniqChannelName()
        {
            return Guid.NewGuid().ToString().Substring(0, 6);
        }
    }
}