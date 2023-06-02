using System;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShaderBuilder;
public enum MessageType
{
	Error,
	Warning,
	Message,
}

public class BuildEventArgsConverter : JsonConverter<BuildEventArgs>
{
	public override BuildEventArgs ReadJson(JsonReader reader, Type objectType, BuildEventArgs existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (hasExistingValue)
		{
			throw new NotImplementedException();
		}

		var obj = JObject.Load(reader);
		return (MessageType)obj["Type"].Value<int>() switch
		{
			MessageType.Error => new BuildErrorEventArgs(
				obj[nameof(BuildErrorEventArgs.Subcategory)].Value<string>(),
				obj[nameof(BuildErrorEventArgs.Code)].Value<string>(),
				obj[nameof(BuildErrorEventArgs.File)].Value<string>(),
				obj[nameof(BuildErrorEventArgs.LineNumber)].Value<int>(),
				obj[nameof(BuildErrorEventArgs.ColumnNumber)].Value<int>(),
				obj[nameof(BuildErrorEventArgs.EndColumnNumber)].Value<int>(),
				obj[nameof(BuildErrorEventArgs.EndColumnNumber)].Value<int>(),
				obj[nameof(BuildErrorEventArgs.Message)].Value<string>(),
				obj[nameof(BuildErrorEventArgs.HelpKeyword)].Value<string>(),
				obj[nameof(BuildErrorEventArgs.SenderName)].Value<string>(),
				obj[nameof(BuildErrorEventArgs.Timestamp)].ToObject<DateTime>()),
			MessageType.Warning => new BuildWarningEventArgs(
				obj[nameof(BuildWarningEventArgs.Subcategory)].Value<string>(),
				obj[nameof(BuildWarningEventArgs.Code)].Value<string>(),
				obj[nameof(BuildWarningEventArgs.File)].Value<string>(),
				obj[nameof(BuildWarningEventArgs.LineNumber)].Value<int>(),
				obj[nameof(BuildWarningEventArgs.ColumnNumber)].Value<int>(),
				obj[nameof(BuildWarningEventArgs.EndColumnNumber)].Value<int>(),
				obj[nameof(BuildWarningEventArgs.EndColumnNumber)].Value<int>(),
				obj[nameof(BuildWarningEventArgs.Message)].Value<string>(),
				obj[nameof(BuildWarningEventArgs.HelpKeyword)].Value<string>(),
				obj[nameof(BuildWarningEventArgs.SenderName)].Value<string>(),
				obj[nameof(BuildWarningEventArgs.Timestamp)].ToObject<DateTime>()),
			MessageType.Message => new BuildMessageEventArgs(
				obj[nameof(BuildMessageEventArgs.Message)].Value<string>(),
				obj[nameof(BuildMessageEventArgs.HelpKeyword)].Value<string>(),
				obj[nameof(BuildMessageEventArgs.SenderName)].Value<string>(),
				(MessageImportance)obj[nameof(BuildMessageEventArgs.Importance)].Value<int>(),
				obj[nameof(BuildMessageEventArgs.Timestamp)].ToObject<DateTime>()),
			_ => throw new NotImplementedException(),
		};
	}

	public override void WriteJson(JsonWriter writer, BuildEventArgs value, JsonSerializer serializer)
	{
		var json = JObject.FromObject(value);
		json.Add(new JProperty("Type", value switch
		{
			BuildMessageEventArgs => MessageType.Message,
			BuildWarningEventArgs => MessageType.Warning,
			BuildErrorEventArgs => MessageType.Error,
			_ => throw new NotImplementedException()
		}));
		json.WriteTo(writer);
	}
}