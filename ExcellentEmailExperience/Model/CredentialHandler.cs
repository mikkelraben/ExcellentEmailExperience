using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.System;

namespace ExcellentEmailExperience.Model
{
    public class CredentialHandler
    {
        readonly static Windows.Security.Credentials.PasswordVault vault = new Windows.Security.Credentials.PasswordVault();

        protected CredentialHandler() { }

        public static string GetCredential(string email)
        {
            try
            {
                return vault.Retrieve("SoftwareGroup1.ExcellentEmailExperience", email).Password;
            }
            catch (Exception)
            {
                if (email != "This account name cannot be used")
                    MessageHandler.AddMessage($"Could not get credential for email {email}", MessageSeverity.Info);
                return null;
            }
        }

        public static void AddCredential(string email, string secret)
        {
            try
            {
                vault.Add(new Windows.Security.Credentials.PasswordCredential("SoftwareGroup1.ExcellentEmailExperience", email, secret));
            }
            catch (Exception)
            {
                MessageHandler.AddMessage($"Could not add credential for email {email}", MessageSeverity.Info);
                throw;
            }
        }

        public static void RemoveCredential(string email)
        {
            var credential = vault.FindAllByUserName(email).FirstOrDefault();
            if (credential != null)
            {
                vault.Remove(credential);
            }
        }

        public static string[] GetAccounts()
        {
            try
            {
                return vault.FindAllByResource("SoftwareGroup1.ExcellentEmailExperience").Select(x => x.UserName).ToArray();
            }
            catch (Exception)
            {
                MessageHandler.AddMessage("Could not get accounts", MessageSeverity.Info);
                return [];
            }
        }
    }

    internal class CredentialHandlerGoogleShim : Google.Apis.Util.Store.IDataStore
    {
        public async Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync<T>(string key)
        {
            CredentialHandler.RemoveCredential(key);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var credential = CredentialHandler.GetCredential(key);
            if (credential == null)
            {
                return default;
            }
            T? thing = JsonSerializer.Deserialize<T>(credential);
            return thing;
        }

        public async Task StoreAsync<T>(string key, T value)
        {
            CredentialHandler.AddCredential(key, JsonSerializer.Serialize(value));
        }
    }
}


