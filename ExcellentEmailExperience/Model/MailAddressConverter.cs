using System;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MailAddressConverter : JsonConverter<MailAddress>
{
    public override MailAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string address = "";
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.PropertyName:
                    switch (reader.GetString())
                    {
                        case "Address":
                            reader.Read();
                            address = reader.GetString();
                            break;
                    }
                    break;
                case JsonTokenType.EndObject:
                    return new MailAddress(address);
            }
        }


        return new MailAddress("");
    }

    public override void Write(Utf8JsonWriter writer, MailAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
