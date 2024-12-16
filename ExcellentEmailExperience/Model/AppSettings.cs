using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExcellentEmailExperience.Model
{
    public enum Theme
    {
        System,
        Light,
        Dark
    }

    public class AppSettings
    {
        [JsonInclude]
        public Theme theme;

        [JsonInclude]
        public List<string> signatures;

        [JsonInclude]
        public MessageSeverity logLevel;

        [JsonInclude]
        public int mailFetchCount;



        public void addSignature(string signature)
        {
            signatures.Add(signature);
        }

        public void removeSignature(int index)
        {
            signatures.RemoveAt(index);
        }

    }
}
