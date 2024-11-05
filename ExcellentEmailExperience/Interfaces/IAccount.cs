namespace ExcellentEmailExperience.Interfaces
{
    public interface IAccount
    {
        /// <summary>
        /// Logs in the user this function may throw an exception if the login fails
        /// </summary>
        /// <param name="username"></param>
        void Login(string username);

        /// <summary>
        /// Tries to login with the given credentials
        /// Performs a login and returns if the login was successful
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="secret">Either a token or password</param>
        /// <returns>Returns if </returns>
        bool TryLogin(string username, string secret);
        void Logout();
        IMailHandler GetMailHandler();
    }
}
