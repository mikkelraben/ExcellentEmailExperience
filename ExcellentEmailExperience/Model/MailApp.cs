using ExcellentEmailExperience.Interfaces;
using Google;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace ExcellentEmailExperience.Model
{
    public class MailApp
    {
        private readonly List<IAccount> accounts = new();

        AppSettings settings;

        public List<IAccount> Accounts { get => accounts; }

        public MailApp()
        {
        }

        public async Task Initialize()
        {
            LoadAppSettings();
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

        public void DeleteAccount(IAccount account)
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
                    try
                    {
                        accounts.Add(account);
                        account.Login(account.GetEmail().Address);
                    }
                    catch (GoogleApiException e)
                    {
                        MessageHandler.AddMessage("Could not connect to Google", MessageSeverity.Error);
                    }
                    catch (Exception e)
                    {
                        MessageHandler.AddMessage("Error loading accounts", MessageSeverity.Error);
                    }
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

        private async Task LoadAppSettings()
        {
            try
            {
                StorageFile file = ApplicationData.Current.LocalFolder.GetFileAsync("settings.json").AsTask().Result;
                settings = JsonSerializer.Deserialize<AppSettings>(FileIO.ReadTextAsync(file).AsTask().Result);
            }
            catch (FileNotFoundException e)
            {
                MessageHandler.AddMessage("No settings file found, creating new settings file", MessageSeverity.Warning);
                settings = new AppSettings();
            }
        }

        public void SaveAppSettings()
        {
            string output = JsonSerializer.Serialize(settings);
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = folder.CreateFileAsync("settings.json", CreationCollisionOption.ReplaceExisting).AsTask().Result;
            FileIO.WriteTextAsync(file, output).AsTask().Wait();
        }
    }
}
