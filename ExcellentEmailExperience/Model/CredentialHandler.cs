using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    internal class CredentialHandler
    {
        readonly static Windows.Security.Credentials.PasswordVault vault = new Windows.Security.Credentials.PasswordVault();

        protected CredentialHandler() { }

        public static string GetCredential(string username)
        {
            try
            {
                return vault.Retrieve("SoftwareGroup1.ExcellentEmailExperience", username).Password;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void AddCredential(string username, string secret)
        {
            vault.Add(new Windows.Security.Credentials.PasswordCredential("SoftwareGroup1.ExcellentEmailExperience", username, secret));
        }

        public static void RemoveCredential(string username)
        {
            var credential = vault.FindAllByUserName(username).FirstOrDefault();
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
            Console.WriteLine(thing);
            return thing;
        }

        public async Task StoreAsync<T>(string key, T value)
        {
            CredentialHandler.AddCredential(key, JsonSerializer.Serialize(value));
        }
    }
}


