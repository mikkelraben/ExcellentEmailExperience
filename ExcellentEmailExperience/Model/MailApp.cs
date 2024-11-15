using ExcellentEmailExperience.Interfaces;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace ExcellentEmailExperience.Model
{
    public class MailApp
    {
        private readonly List<IAccount> accounts = new();

        public List<IAccount> Accounts { get => accounts; }

        public MailApp()
        {

        }

        public async Task Initialize()
        {
            await LoadAccounts();
        }

        /// <summary>
        /// Creates a new account
        /// </summary>
        /// <param name="account">The account to add to the mail app</param>
        public void NewAccount(IAccount account)
        {
            accounts.Add(account);
        }

        public void DeleteAccount()
        {
            throw new NotImplementedException();
        }

        public bool HasAccount()
        {
            return accounts.Count > 0;
        }

        private async Task LoadAccounts()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new MailAddressConverter() }
            };

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync("accounts.json");
                JsonSerializer.Deserialize<List<IAccount>>(FileIO.ReadTextAsync(file)
                    .AsTask().Result, options).ForEach(account =>
                {
                    account.Login(account.GetEmail());
                    accounts.Add(account);

                });
            }
            catch (FileNotFoundException e)
            {
                // No accounts.json file found so we don't need to do anything
            }
            return;
        }

        public void SaveAccounts()
        {
            string output = JsonSerializer.Serialize(accounts);

            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = folder.CreateFileAsync("accounts.json", CreationCollisionOption.ReplaceExisting).AsTask().Result;
            FileIO.WriteTextAsync(file, output).AsTask().Wait();
        }
    }
}
