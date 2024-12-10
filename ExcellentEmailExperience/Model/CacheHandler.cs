﻿using Microsoft.Data.Sqlite;
using System;
using System.IO;
using Windows.Storage;
using System.Net.Mail;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using ExcellentEmailExperience.Interfaces;
using System.Text.Json;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace ExcellentEmailExperience.Model
{
    public class CacheHandler
    {
        // in all Googles gloriousness they have decided to send the same message multiple times, which throws an exception if the caching is not done quickly enough
        // this is a means of counteracting that to give the cache more time by having its own cache, cache jr. (legal name: shortTermCache)
        private List<string> shortTermCache = new();
        private string connectionString;

        public static Dictionary<string, List<string>> ParseMailQuery(string query)
        {
            // Dictionary to store the parsed fields
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            // Regex to match fields like "from:", "cc:", "subject:", followed by their values
            string pattern = @"(?<field>from|cc|bcc|subject|to|after|before|newer|older|older_than|newer_than|flag|has|filename|is):(?<value>.*?)(?=(\s+\w+:|$))";
            var matches = Regex.Matches(query, pattern, RegexOptions.IgnoreCase);

            // To track the end of the last match
            int lastMatchEnd = 0;

            foreach (Match match in matches)
            {
                string field = match.Groups["field"].Value.ToLower();
                string value = match.Groups["value"].Value.Trim();

                if (!result.ContainsKey(field))
                {
                    result[field] = new List<string>();
                }

                result[field].Add(value);

                // Update last match end position
                lastMatchEnd = match.Index + match.Length;
            }

            // Capture free text after the last matched field
            if (lastMatchEnd < query.Length)
            {
                string freeText = query.Substring(lastMatchEnd).Trim();
                if (!string.IsNullOrEmpty(freeText))
                {
                    // Store free text under a default field (e.g., "search")
                    const string defaultField = "search";
                    if (!result.ContainsKey(defaultField))
                    {
                        result[defaultField] = new List<string>();
                    }
                    result[defaultField].Add(freeText);
                }
            }

            return result;
        }

        public CacheHandler(string accountAddress)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            string folderPath = $@"{folder.Path}\data";

            string DBName = Convert.ToBase64String(Encoding.UTF8.GetBytes(accountAddress)).Replace('/', '-');

            string filePath = $@"{folderPath}\{DBName}.db";
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
                        flags INTEGER,
	                    PRIMARY KEY(MessageId)
                        );";
                        command.ExecuteNonQuery();
                    }
                }
            }

            UpdateTable();
        }

        /// <summary>
        /// Adds backwards compatibility to the flags update. Should probs be removed or at least updated cus it be very cursed
        /// </summary>
        private void UpdateTable()
        {
            List<string> columnNames = new List<string>();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        // add the column if it doesn't exist
                        command.CommandText = $@"
                        ALTER TABLE MailContent
                        ADD COLUMN flags INTEGER;
                        ";
                        command.ExecuteNonQuery();

                        // fill it with elements
                        command.CommandText = $@"
                        UPDATE MailContent
                        SET flags = 0
                        ";
                        command.ExecuteNonQuery();

                        // fix the FolderIds
                        command.CommandText = $@"
                        UPDATE MailContent
                        SET FolderId = $folder
                        ";
                        var folder = new List<string>();
                        command.Parameters.AddWithValue("$folder", JsonSerializer.Serialize(folder));
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqliteException ex)
                {
                    if (ex.SqliteErrorCode == 1)
                    {
                        // table already exists
                    }
                    else
                    {
                        throw new Exception(ex.Message);
                    }
                }
            }

        }

        public void CacheMessage(MailContent mail, string folderName)
        {
            if (shortTermCache.Contains(mail.MessageId)) return;
            shortTermCache.Add(mail.MessageId);

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                    $@"
                    INSERT INTO MailContent
                    (MessageId, [from], [to], bcc, cc, bodytype, subject, body, attachments, date, ThreadId, FolderId, flags)
                    VALUES ($id, $from, $to, $bcc, $cc, $bodytype, $subject, $body, $attach, $date, $thread, $folder, $flags);
                    ";
                    command.Parameters.AddWithValue("$id", mail.MessageId);
                    command.Parameters.AddWithValue("$from", JsonSerializer.Serialize(mail.from));
                    command.Parameters.AddWithValue("$to", JsonSerializer.Serialize(mail.to));
                    command.Parameters.AddWithValue("$bcc", JsonSerializer.Serialize(mail.bcc));
                    command.Parameters.AddWithValue("$cc", JsonSerializer.Serialize(mail.cc));
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
                    command.Parameters.AddWithValue("$attach", attach);
                    command.Parameters.AddWithValue("$date", mail.date.ToString());
                    command.Parameters.AddWithValue("$thread", mail.ThreadId);

                    List<string> folders = new List<string> { folderName };
                    command.Parameters.AddWithValue("$folder", JsonSerializer.Serialize(folders));
                    command.Parameters.AddWithValue("$flags", (int)mail.flags);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        public MailContent GetCache(string messageId)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new MailAddressConverter() }
            };

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = $@"
                SELECT *
                FROM MailContent
                WHERE MessageId = $id
                ";
                command.Parameters.AddWithValue("$id", messageId);

                var reader = command.ExecuteReader();
                reader.Read();

                MailContent mail = new MailContent();
                mail.MessageId = reader.GetString(0);
                mail.from = JsonSerializer.Deserialize<MailAddress>(reader.GetString(1), options);
                mail.to = JsonSerializer.Deserialize<List<MailAddress>>(reader.GetString(2), options);
                mail.bcc = JsonSerializer.Deserialize<List<MailAddress>>(reader.GetString(3), options);
                mail.cc = JsonSerializer.Deserialize<List<MailAddress>>(reader.GetString(4), options);
                mail.bodyType = (BodyType)reader.GetInt32(5);
                mail.subject = reader.GetString(6);
                mail.body = reader.GetString(7);

                var attachments = reader.GetString(8);
                if (attachments != "")
                    mail.attachments = reader.GetString(8).Split(';').ToList();

                mail.date = DateTime.Parse(reader.GetString(9));
                mail.ThreadId = reader.GetString(10);
                mail.flags = (MailFlag)reader.GetInt32(12);

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

                try
                {
                    var result = command.ExecuteScalar();
                    bool exists = Convert.ToBoolean(result);
                    return exists;
                }
                catch (Exception ex)
                {
                    throw new Exception("CheckCache Failed");
                }
            }
        }

        public void UpdateFlagsAndFolders(string messageId, string folderName)

                List<string>? folders;
                int? flags;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                    SELECT *
                    FROM MailContent
                    WHERE MessageId = $id
                    ";
                    command.Parameters.AddWithValue("$id", messageId);

                    var reader = command.ExecuteReader();
                    reader.Read();

                    var getstringthing = reader.GetString(11);
                    folders = JsonSerializer.Deserialize<List<string>>(getstringthing);
                    if (!folders.Contains(folderName))
                    {
                        folders.Add(folderName);
                    }

                    flags = reader.GetInt32(12);
                    switch (folderName)
                    {
                        case "UNREAD":  
                            flags |= (int)MailFlag.unread;
                            break;
                        case "STARRED":
                            flags |= (int)MailFlag.favorite;
                            break;
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                    UPDATE MailContent
                    SET FolderId = $folder, flags = $flags
                    WHERE MessageId = $id
                    ";
                    command.Parameters.AddWithValue("$folder", JsonSerializer.Serialize(folders));
                    command.Parameters.AddWithValue("$flags", flags);
                    command.Parameters.AddWithValue("$id", messageId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateFlags(string messageId, MailFlag flag)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
              var command = connection.CreateCommand();
                command.CommandText = $@"
                SELECT *
                FROM MailContent
                WHERE MessageId = $id
                ";
                command.Parameters.AddWithValue("$id", messageId);

                var reader = command.ExecuteReader();
                reader.Read();

                var flags = reader.GetInt32(12);
                flags |= (int)flag;
                reader.Close();

                command.CommandText = $@"
                UPDATE MailContent
                SET flags = $flags
                WHERE MessageId = $id
                ";

                command.Parameters.AddWithValue("$flags", flags);
                command.Parameters.AddWithValue("$id", messageId);
                command.ExecuteNonQuery();
            }
        }


        public void ClearCache()
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "TRUNCATE TABLE MailContent";
                command.ExecuteNonQuery();
            }
        }

        public IEnumerable<MailContent> SearchCache(string query)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new MailAddressConverter() }
            };

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                

                if (query == "") yield break;

                var command = connection.CreateCommand();
                command.CommandText = $@"
                SELECT *
                FROM MailContent
                ";


                Dictionary<string, List<string>> parsedQuery = ParseMailQuery(query);

                var conditions = new List<string>();

                if (parsedQuery.TryGetValue("from", out List<string> from))
                {
                    conditions.Add("[from] LIKE $from");
                }
                if (parsedQuery.TryGetValue("to", out List<string> to))
                {
                    conditions.Add("[to] LIKE $to");
                }
                if (parsedQuery.TryGetValue("cc", out List<string> cc))
                {
                    conditions.Add("cc LIKE $cc");
                }
                if (parsedQuery.TryGetValue("bcc", out List<string> bcc))
                {
                    conditions.Add("bcc LIKE $bcc");
                }
                if (parsedQuery.TryGetValue("subject", out List<string> subject))
                {
                    conditions.Add("subject LIKE $subject");
                }

                // Combine all conditions into a single WHERE clause
                if (conditions.Count > 0)
                {
                    command.CommandText += " WHERE " + string.Join(" AND ", conditions);
                }

                // Add parameters to the command
                foreach (var param in parsedQuery)
                {
                    // Assuming param.Value is a list of strings you want to bind
                    foreach (var value in param.Value)
                    {
                        command.Parameters.AddWithValue($"${param.Key}", value);
                    }
                }



                //command.Parameters.AddWithValue("$folder", folderName);

                var reader = command.ExecuteReader();

                IEnumerable<MailContent> mails = new List<MailContent>();
                while (reader.Read())
                {
                    MailContent mail = new MailContent();
                    mail.MessageId = reader.GetString(0);
                    mail.from = JsonSerializer.Deserialize<MailAddress>(reader.GetString(1), options);
                    mail.to = JsonSerializer.Deserialize<List<MailAddress>>(reader.GetString(2), options);
                    mail.bcc = JsonSerializer.Deserialize<List<MailAddress>>(reader.GetString(3), options);
                    mail.cc = JsonSerializer.Deserialize<List<MailAddress>>(reader.GetString(4), options);
                    mail.bodyType = (BodyType)reader.GetInt32(5);
                    mail.subject = reader.GetString(6);
                    mail.body = reader.GetString(7);

                    var attachments = reader.GetString(8);
                    if (attachments != "")
                        mail.attachments = reader.GetString(8).Split(';').ToList();

                    mail.date = DateTime.Parse(reader.GetString(9));
                    mail.ThreadId = reader.GetString(10);
                    mail.flags = (MailFlag)reader.GetInt32(11);

                    mails.Append(mail);
                    yield return mail;
                }

            }
        }


    }
}
