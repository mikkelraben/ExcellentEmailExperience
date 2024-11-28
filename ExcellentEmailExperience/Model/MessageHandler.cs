using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    /// <summary>
    /// The severity of a message
    /// Note: Info messages are only displayed in debug mode, and only in the console
    /// 
    /// Info - Informational message.
    /// Success - A message which denotes a successful operation, for example a message was sent.
    /// Warning - A message which denotes a potential issue, for example part of a message could not be parsed.
    /// Error - A message which denotes a critical issue ie. the program did not do what the user wanted, for example a message could not be sent, or not connected to the internet.
    /// </summary>
    public enum MessageSeverity
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// A message to be displayed to the user
    /// </summary>
    public class Message
    {
        public string message;
        public MessageSeverity severity;
    }

    static class MessageHandler
    {
        static List<Message> messages = new();

        /// <summary>
        /// This event is called each time a message is added
        /// </summary>
        public static event EventHandler MessageAdded;

        /// <summary>
        /// Add a message to be potentially displayed to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="severity"></param>
        public static void AddMessage(string message, MessageSeverity severity)
        {
            messages.Insert(0, new Message { message = message, severity = severity });
            if (MessageAdded is not null)
                MessageAdded.Invoke(null, EventArgs.Empty);
        }

        public static List<Message> GetMessages()
        {
            return messages;
        }

        public static Message? GetFirstMessage()
        {
            if (messages.Count > 0)
            {
                return messages[0];
            }
            return null;
        }
    }
}
