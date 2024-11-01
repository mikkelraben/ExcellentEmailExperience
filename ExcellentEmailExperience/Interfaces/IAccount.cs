namespace ExcellentEmailExperience.Interfaces
{
    public interface IAccount
    {
        void Login(string username);
        void Logout();
        IMailHandler GetMailHandler();
    }
}
