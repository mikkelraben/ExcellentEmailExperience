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
        public string[] signatures;
        public MessageSeverity logLevel;
        public string mainMail;
        public int mailFetchCount;

    }
}
