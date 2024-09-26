using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;
using System.Collections.Generic;


List<string> one = GetUrls("wwwroot/data/all.json");
List<string> two = GetUrls("wwwroot/data/all.default.json");

one.AddRange(two);

var urls = one.Distinct().ToList();
urls.RemoveAll(s => s.Contains("/artists/"));

List<(string src, string download)> _urls = new();

var mediaJson = File.ReadAllText("wwwroot/data/media.json");
int thumbcount = 0;
foreach (var url in urls)
{
	Uri uri = new Uri(url);

	string[] segments = uri.Segments;

	if (segments.Length > 1)
	{
		string penultimateSegment = segments[segments.Length - 2].Trim('/');
		var found = ExtractContentInQuotesAroundTarget(mediaJson, penultimateSegment);
		if (found is not null)
		{
			_urls.Add((found, url));
			thumbcount++;

		}
		else
		{
			_urls.Add((url, url));

		}
	}
	else
	{
		_urls.Add((url, url));

	}
}

Console.WriteLine($"found {thumbcount} thumbnails!");

JsonSerializerOptions options = new JsonSerializerOptions
{
	WriteIndented = true,
	Converters = { new TupleConverter() },
	Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
var resultUrls = JsonSerializer.Serialize(_urls, options);

File.WriteAllText("wwwroot/data/merge.json", resultUrls, System.Text.Encoding.UTF8);


void ExtractUrls(JsonElement element, List<string> urls)
{
	if (element.ValueKind == JsonValueKind.Object)
	{
		foreach (var property in element.EnumerateObject())
		{
			if (property.Name == "url")
			{
				urls.Add(property.Value.GetString());
			}
			else
			{
				ExtractUrls(property.Value, urls);
			}
		}
	}
	else if (element.ValueKind == JsonValueKind.Array)
	{
		foreach (var item in element.EnumerateArray())
		{
			ExtractUrls(item, urls);
		}
	}
}

List<string> GetUrls(string path)
{
	var json = File.ReadAllText("wwwroot/data/all.json");
	var doc = JsonDocument.Parse(json);
	var resultUrls = new List<string>();
	ExtractUrls(doc.RootElement, resultUrls);
	return resultUrls;
}

string ExtractContentInQuotesAroundTarget(string fullText, string targetString)
{
	int targetIndex = fullText.IndexOf(targetString);
	if (targetIndex != -1)
	{
		// Find the nearest opening quote before the target string
		int startQuoteIndex = fullText.LastIndexOf('"', targetIndex);
		if (startQuoteIndex == -1) return null; // No starting quote found

		// Find the nearest closing quote after the target string
		int endQuoteIndex = fullText.IndexOf('"', targetIndex + targetString.Length);
		if (endQuoteIndex == -1) return null; // No ending quote found

		// Extract the content between the quotes
		return fullText.Substring(startQuoteIndex + 1, endQuoteIndex - startQuoteIndex - 1);
	}

	return null; // Target string not found
}



public class TupleConverter : JsonConverter<(string src, string url)>
{
	public override (string src, string url) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
		{
			string src = doc.RootElement.GetProperty("Item1").GetString();
			string url = doc.RootElement.GetProperty("Item2").GetString();
			return (src, url);
		}
	}

	public override void Write(Utf8JsonWriter writer, (string src, string url) value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("src", value.src);
		writer.WriteString("download", value.url);
		writer.WriteEndObject();
	}
}