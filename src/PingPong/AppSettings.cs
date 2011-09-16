using System.IO.IsolatedStorage;

namespace PingPong
{
    public static class AppSettings
    {
        public static bool HasAuthToken
        {
            get { return UserOAuthToken != null && UserOAuthTokenSecret != null; }
        }

        public static string UserOAuthToken
        {
            get
            {
                string value;
                IsolatedStorageSettings.ApplicationSettings.TryGetValue("pp.user.token", out value);
                return value;
            }
            set { IsolatedStorageSettings.ApplicationSettings["pp.user.token"] = value; }
        }

        public static string UserOAuthTokenSecret
        {
            get
            {
                string value;
                IsolatedStorageSettings.ApplicationSettings.TryGetValue("pp.user.secret", out value);
                return value;
            }
            set { IsolatedStorageSettings.ApplicationSettings["pp.user.secret"] = value; }
        }
    }
}