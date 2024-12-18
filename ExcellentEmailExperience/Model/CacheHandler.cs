using Microsoft.Data.Sqlite;
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
using System.Globalization;

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
            string path;
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                path = folder.Path;
            }
            catch (Exception)
            {
                path = Directory.GetCurrentDirectory();
            }
            string folderPath = $@"{path}\data";

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
                    command.Parameters.AddWithValue("$date", mail.date.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("$thread", mail.ThreadId);

                    List<string> folders = new List<string> { folderName };
                    command.Parameters.AddWithValue("$folder", JsonSerializer.Serialize(folders));
                    command.Parameters.AddWithValue("$flags", (int)mail.flags);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        /// <summary>
        /// Helper function that rebuilds a MailContent given a row in the database.
        /// </summary>
        /// <param name="reader"> Sqlite reader which holds onto information from a row in the database </param>
        /// <returns> Mailcontent corresponding to what the reader reads </returns>
        private MailContent BuildMailContent(SqliteDataReader reader)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new MailAddressConverter() }
            };

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

            mail.date = DateTime.ParseExact(reader.GetString(9), "yyyy-MM-dd HH:mm:ss", null);
            mail.ThreadId = reader.GetString(10);
            mail.flags = (MailFlag)reader.GetInt32(12);

            return mail;
        }

        /// <summary>
        /// Function that returns a selected MailContent from the database based on a messageId.
        /// </summary>
        /// <param name="messageId"> The id of the requested message. </param>
        /// <returns> MailContent corresponding to the inputted messageId </returns>
        public MailContent GetMessage(string messageId)
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

                return BuildMailContent(reader);
            }
        }

        /// <summary>
        /// Retrieves the newest mails stored in the database from a specific folder.
        /// </summary>
        /// <param name="folderName"> The folder, which mails are retrieved from </param>
        /// <param name="count"> The amount of mails retrieved. Given no coun parameter, the function returns the 20 newest mails by default. </param>
        /// <returns> List containing the newest mails stored as MailContent </returns>
        public List<MailContent> GetFolder(string folderName, int count = 20)
        {
            List<MailContent> mailList = new();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT * 
                FROM MailContent
                WHERE FolderId LIKE $folder
                ORDER BY date DESC
                LIMIT $limit;
                ";
                command.Parameters.AddWithValue("$folder", "%" + folderName + "%");
                command.Parameters.AddWithValue("$limit", count);

                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    mailList.Add(BuildMailContent(reader));
                }
            }

            return mailList;
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

        /// <summary>
        /// A function that adds a folder (<paramref name="folderName"/>) belonging to a mail (<paramref name="messageId"/>) to the cache.
        /// It also sets the flag to true if the folder indicates as such (e.g. if the folder is UNREAD).
        /// </summary>
        /// <param name="messageId"> Id for the mail which is also the SQL table's row id </param>
        /// <param name="folderName"> FolderId which should be added to cache for the given <paramref name="messageId"/> </param>
        /// <param name="flagDict"> Dictionary that translates the folderId of folders containing flagging info into the corresponding MailFlag value </param>
        /// <param name="inbox"> Inbox folder id, which is used to remove mail from inbox when it is added to trash or spam </param>
        public void AddFolder(string messageId, string folderName, Dictionary<string, MailFlag> flagDict, string inbox)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                List<string>? folderList = new();
                int? flagList = 0;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                    SELECT FolderId, flags
                    FROM MailContent
                    WHERE MessageId = $id
                    ";
                    command.Parameters.AddWithValue("$id", messageId);

                    try
                    {
                        var reader = command.ExecuteReader();
                        reader.Read();
                        folderList = JsonSerializer.Deserialize<List<string>>(reader.GetString(0));
                        if (!folderList.Contains(folderName))
                        {
                            folderList.Add(folderName);

                            // in the backend of gmail it seems like only inbox label is removed when things become spam/trash
                            // this mimics the same behavior, but idk how other email servers implement this
                            bool updateInbox = flagDict.ContainsKey(folderName) && (flagDict[folderName] == MailFlag.spam || flagDict[folderName] == MailFlag.trash) && folderList.Contains(inbox);
                            if (updateInbox)
                            {
                                folderList.Remove(inbox);
                            }
                        }

                        flagList = reader.GetInt32(1);
                        if (flagDict.ContainsKey(folderName))
                        {
                            flagList |= (int)flagDict[folderName];
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Reader says: " + ex.Message);
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                    UPDATE MailContent
                    SET FolderId = $folders, flags = $flags
                    WHERE MessageId = $id
                    ";
                    command.Parameters.AddWithValue("$folders", JsonSerializer.Serialize(folderList));
                    command.Parameters.AddWithValue("$flags", flagList);
                    command.Parameters.AddWithValue("$id", messageId);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NonQuery sys: " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// A function that is very similar to <see cref="AddFolder(string, string, Dictionary{string, MailFlag}, string)"/>.
        /// The difference is that this function only removes folders from cache and sets corresponding flags to false.
        /// </summary>
        /// <param name="messageId"> Id for the mail which is also the SQL table's row id </param>
        /// <param name="folderName"> FolderId which should be removed from cache for the given <paramref name="messageId"/> </param>
        /// <param name="flagDict"> Dictionary that translates the folderId of folders containing flagging info into the corresponding MailFlag value </param>
        /// <param name="inbox"> Inbox folder id, which is used to add mail to inbox when it is removed from trash or spam </param>
        public void RemoveFolder(string messageId, string folderName, Dictionary<string, MailFlag> flagDict, string inbox)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                List<string>? folderList = new();
                int? flagList = 0;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                    SELECT FolderId, flags
                    FROM MailContent
                    WHERE MessageId = $id
                    ";
                    command.Parameters.AddWithValue("$id", messageId);

                    var reader = command.ExecuteReader();
                    reader.Read();

                    folderList = JsonSerializer.Deserialize<List<string>>(reader.GetString(0));
                    if (folderList.Contains(folderName))
                    {
                        folderList.Remove(folderName);

                        bool updateInbox = flagDict.ContainsKey(folderName) && (flagDict[folderName] == MailFlag.spam || flagDict[folderName] == MailFlag.trash) && folderList.Contains(inbox);
                        if (updateInbox)
                        {
                            folderList.Add(inbox);
                        }
                    }

                    flagList = reader.GetInt32(1);
                    if (flagDict.ContainsKey(folderName))
                    {
                        flagList &= ~(int)flagDict[folderName];
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                    UPDATE MailContent
                    SET FolderId = $folders, flags = $flags
                    WHERE MessageId = $id
                    ";
                    command.Parameters.AddWithValue("$folders", JsonSerializer.Serialize(folderList));
                    command.Parameters.AddWithValue("$flags", flagList);
                    command.Parameters.AddWithValue("$id", messageId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateFolder(List<string> newIdList, string folderName, Dictionary<string, MailFlag> flagDict, string inbox)
        {
            List<string> oldIdList = new();
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = $@"
                SELECT MessageId
                FROM MailContent
                WHERE FolderId LIKE $folder
                ";
                command.Parameters.AddWithValue("$folder", "%" + folderName + "%");

                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    oldIdList.Add(reader.GetString(0));
                }
            }

            var deltaIdList = oldIdList.Except(newIdList);

            foreach (var id in deltaIdList)
            {
                RemoveFolder(id, folderName, flagDict, inbox);
            }
        }

        public void ClearRow(string messageId)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                DELETE
                FROM MailContent
                WHERE MessageId = $id";
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
                    mail.flags = (MailFlag)reader.GetInt32(12);

                    mails.Append(mail);
                    yield return mail;
                }

            }
        }
    }
}
