using ExcellentEmailExperience.Model;
using System.Text.Json.Serialization;

namespace ExcellentEmailExperience.Interfaces
{
    [JsonPolymorphic(
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
    [JsonDerivedType(typeof(GmailAccount), typeDiscriminator: "gmail")]
    public interface IAccount
    {
        /// <summary>
        /// Logs in the user this function may throw an exception if the login fails
        /// </summary>
        /// <param name="email"></param>
        void Login(string email);

        /// <summary>
        /// Tries to login with the given credentials
        /// Performs a login and returns if the login was successful
        /// </summary>
        /// <param name="email">The username</param>
        /// <param name="secret">Either a token or password</param>
        /// <returns>Returns true if login was successful</returns>
        bool TryLogin(string email, string secret);
        void Logout();
        IMailHandler GetMailHandler();

        /// <summary>
        /// Returns the display name of the account
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// Sets the display name of the account
        /// </summary>
        /// <param name="name">The name to be displayed</param>
        void SetName(string name);

        /// <summary>
        /// Returns the email address of the account
        /// </summary>
        /// <returns></returns>
        string GetEmail();
    }
}
