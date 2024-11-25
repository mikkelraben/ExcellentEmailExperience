using Microsoft.Data.Sqlite;
using System;
using System.IO;
using Windows.Storage;
using System.Net.Mail;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace ExcellentEmailExperience.Model
{
    public class CacheHandler
    {
        private string connectionString;
        
        public CacheHandler(string accountAddress)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string folderPath = $@"{folder.Path}\data";
            string filePath = $@"{folderPath}\{accountAddress}.db";
            connectionString = $@"Data Source={filePath}";

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if (!File.Exists(filePath))
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();

                    // Create the tables
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $@"
                        CREATE TABLE IF NOT EXISTS MailContent (
	                    MessageId TEXT NOT NULL UNIQUE,
	                    [from] TEXT NOT NULL,
	                    [to] TEXT NOT NULL,
	                    bcc TEXT,
	                    cc TEXT,
	                    bodytype INTEGER,
	                    subject TEXT,
	                    body TEXT,
	                    attachments TEXT,
	                    date TEXT NOT NULL,
	                    ThreadId TEXT,
                        FolderId TEXT NOT NULL,
	                    PRIMARY KEY(MessageId)
                        );";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        // THIS DOES NOT WORK AT ALL
        public void CacheMessage(MailContent mail, string folderName)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                    $@"
                    INSERT INTO MailContent
                    (MessageId, [from], [to], bcc, cc, bodytype, subject, body, attachments, date, ThreadId, FolderId)
                    VALUES ($id, $from, $to, $bcc, $cc, $bodytype, $subject, $body, $attach, $date, $thread, $folder)
                    ";
                    command.Parameters.AddWithValue("$id", mail.MessageId);
                    command.Parameters.AddWithValue("$from", mail.from.Address);

                    string to = "";
                    for (int i = 0; i < mail.to.Count; i++)
                    {
                        to += mail.to[i].ToString();
                        if (i + 1 < mail.to.Count)
                        {
                            to += ";";
                        }
                    }
                    to.Remove(to.Length - 1);
                    command.Parameters.AddWithValue("$to", to);

                    string bcc = "";
                    for (int i = 0; i < mail.bcc.Count; i++)
                    {
                        bcc += mail.bcc[i].ToString();
                        if (i + 1 < mail.bcc.Count)
                        {
                            bcc += ";";
                        }
                    }
                    bcc.Remove(bcc.Length - 1);
                    command.Parameters.AddWithValue("$bcc", bcc);

                    string cc = "";
                    for (int i = 0; i < mail.cc.Count; i++)
                    {
                        cc += mail.cc[i].ToString();
                        if (i + 1 < mail.cc.Count)
                        {
                            cc += ";";
                        }
                    }
                    cc.Remove(cc.Length - 1);
                    command.Parameters.AddWithValue("$cc", cc);

                    command.Parameters.AddWithValue("$bodytype", (int)mail.bodyType);
                    command.Parameters.AddWithValue("$subject", mail.subject);
                    command.Parameters.AddWithValue("$body", mail.body);

                    string attach = "";
                    for (int i = 0; i < mail.attachments.Count; i++)
                    {
                        attach += mail.attachments[i];
                        if (i + 1 < mail.attachments.Count)
                        {
                            attach += ";";
                        }
                    }
                    attach.Remove(attach.Length - 1);
                    command.Parameters.AddWithValue("$attach", attach);

                    command.Parameters.AddWithValue("$date", mail.date.ToString());
                    command.Parameters.AddWithValue("$thread", mail.ThreadId);
                    command.Parameters.AddWithValue("$folder", folderName);

                    command.ExecuteNonQuery();
                }
            }
        }

        public MailContent GetCache(string messageId)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = $@"
                SELECT MessageId
                FROM MailContent
                WHERE MessageId = $id
                ";
                command.Parameters.AddWithValue("$id", messageId);

                var reader = command.ExecuteReader();
                reader.Read();

                MailContent mail = new MailContent();
                mail.from = new MailAddress(reader.GetString(1));

                foreach (string address in reader.GetString(2).Split(';'))
                {
                    mail.to.Add(new MailAddress(address));
                }
                foreach (string address in reader.GetString(3).Split(';'))
                {
                    mail.bcc.Add(new MailAddress(address));
                }
                foreach (string address in reader.GetString(4).Split(';'))
                {
                    mail.cc.Add(new MailAddress(address));
                }

                mail.bodyType = (BodyType)reader.GetInt32(5);
                mail.subject = reader.GetString(6);
                mail.body = reader.GetString(7);
                mail.attachments = reader.GetString(2).Split(';').ToList();
                mail.date = DateTime.Parse(reader.GetString(9));
                mail.ThreadId = reader.GetString(10);

                return mail;
            }
        }

        public bool CheckCache(string messageId)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText =
                $@"
                SELECT EXISTS (
                    SELECT 1
                    FROM MailContent
                    WHERE MessageId = $id
                )";
                command.Parameters.AddWithValue("$id", messageId);

                var result = command.ExecuteScalar();
                bool exists = Convert.ToBoolean(result);

                return exists;
            }
        }
    }
}
