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
        public Theme theme;
        public List<string> signatures;
        public MessageSeverity logLevel;
        public string mainMail;
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
