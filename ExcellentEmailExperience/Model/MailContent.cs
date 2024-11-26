using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    public enum BodyType
    {
        Plain,
        Html
    }

    public class MailContent
    {
        public MailAddress? from;
        public List<MailAddress> to = new();
        public List<MailAddress> bcc = new();
        public List<MailAddress> cc = new();
        public BodyType bodyType = BodyType.Plain;
        public string subject = "";
        public string body = "";

        // List of paths to attachments
        public List<string> attachments = new();
        public DateTime date = new();
        public string ThreadId = "";
        public string MessageId = "";

        // for some reason if we dont define how to compare this class. the test code shits itself.
        public override bool Equals(object? obj)
        {
            if (obj is not MailContent other)
                return false;

            return
                Equals(from, other.from) &&
                to.Count == other.to.Count && to.TrueForAll(other.to.Contains) &&
                bcc.Count == other.bcc.Count && bcc.TrueForAll(other.bcc.Contains) &&
                cc.Count == other.cc.Count && cc.TrueForAll(other.cc.Contains) &&
                bodyType == other.bodyType &&
                subject == other.subject &&
                body == other.body &&
                attachments.Count == other.attachments.Count && attachments.TrueForAll(other.attachments.Contains) &&
                date == other.date &&
                ThreadId == other.ThreadId &&
                MessageId == other.MessageId;
        }

        public static bool operator ==(MailContent? left, MailContent? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }
        public static bool operator !=(MailContent? left, MailContent? right)
        {
            if (left is null) return right is not null;
            return !left.Equals(right);
        }

    }
}
