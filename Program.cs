using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Globalization;

class Program
{
  static readonly string apiUrl = "https://api.ocr.space/parse/image";
  static readonly string apikey = "K86678320488957";
  static readonly string filePath = @"files";

  static async Task Main(string[] args)
  {
    var files = Directory.GetFiles(filePath);
    foreach (var file in files)
    {
      string text = await GetTextFromPdf(file);
      string fileName = Path.GetFileNameWithoutExtension(file);
      BuildJson(fileName, text);
    }
  }

  public static async Task<string> GetTextFromPdf(string filePath)
  {
    HttpClient client = new();
    client.DefaultRequestHeaders.Add("apikey", apikey);
    var form = new MultipartFormDataContent();
    var fileStream = System.IO.File.OpenRead(filePath);
    var fileContent = new StreamContent(fileStream);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
    form.Add(fileContent, "file", "arquivo.pdf");
    HttpResponseMessage response = await client.PostAsync(apiUrl, form);
    response.EnsureSuccessStatusCode();

    string responseData = await response.Content.ReadAsStringAsync();
    JsonDocument doc = JsonDocument.Parse(responseData);
    var parsedResults = doc.RootElement.GetProperty("ParsedResults");
    string textResult = parsedResults[0].GetProperty("ParsedText").GetString()
      ?? throw new Exception("No text found in the PDF");

    return textResult;
  }

  public static void BuildJson(string fileName, string text)
  {
    string InvoiceNumber = ExtractInfo(text, "Invoice Number:", "Exporter company");
    string Date = ExtractInfo(text, "Date:", "(yyyy/MM/dd)");
    string BilledTo = ExtractInfo(text, "Billed to", "Country");
    string BusinessNumberInBR = ExtractInfo(text, "Business number (in Brazil):\r", "\r");
    Details[] details = ExtractDataTable(text);

    InfoJson mountJson = new()
    {
      InvoiceNumber = InvoiceNumber,
      Date = Date,
      BilledTo = BilledTo,
      BusinessNumberInBR = BusinessNumberInBR,
      Details = details
    };

    string pathToSave = @"json_files/";
    if (!Directory.Exists(pathToSave))
    {
      Directory.CreateDirectory(pathToSave);
    }

    var JsonData = JsonSerializer.Serialize(mountJson, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(pathToSave + fileName + ".json", JsonData);
  }

  public static string ExtractInfo(string text, string start, string end)
  {
    int startIndex = text.IndexOf(start) + start.Length;
    int endIndex = text.IndexOf(end, startIndex);
    return text[startIndex..endIndex].Trim();
  }

  public static string[] GetService(string text)
  {
    string service = ExtractInfo(text, "Service", "Payment details");
    bool containsDate = service.Contains("Date:");
    if (containsDate)
    {
      return ExtractInfo(text, "Service", "Date:").Split("\r\n");
    }
    else return service.Split("\r\n");
  }

  public static Details[] ExtractDataTable(string text)
  {
    string[] service = GetService(text);
    int dataTableStartIndex = text.IndexOf("Business number (in Brazil):\r") + 48;
    int dataTableEndIndex = text.IndexOf("Additional information", dataTableStartIndex);
    string[] lines = text[dataTableStartIndex..dataTableEndIndex].Split("\r\n");
    // excluir linhas em branco
    lines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
    Details[] data = new Details[service.Length];

    string currentTransaction = null;
    var transactions = new Dictionary<string, List<string>>();

    foreach (var line in lines)
    {
      if (line.StartsWith("S"))
      {
        // Adiciona o item ao array da transação atual
        if (currentTransaction != null)
        {
          if (!transactions.ContainsKey(currentTransaction))
          {
            transactions[currentTransaction] = new List<string>();
          }

          transactions[currentTransaction].Add(line.Replace(" ", "").Replace("S", "").Replace("o", "0").Replace("O", "0").Trim());
        }
      }
      else
      {
        // Linha não começa com "S" é o nome de uma nova transação
        currentTransaction = line;
        if (!transactions.ContainsKey(currentTransaction))
        {
          transactions[currentTransaction] = new List<string>();
        }
      }
    }

    for (int i = 0; i < service.Length; i++)
    {
      data[i] = new()
      {
        Service = service[i],
        OnUSCheck = transactions.ContainsKey("On Us Check") ? transactions["On Us Check"].ElementAtOrDefault(i) ?? "0" : "0",
        CashIn = transactions.ContainsKey("Cash In") ? transactions["Cash In"].ElementAtOrDefault(i) ?? "0" : "0",
        CashOut = transactions.ContainsKey("Cash Out") ? transactions["Cash Out"].ElementAtOrDefault(i) ?? "0" : "0",
        NotOnUSCheck = transactions.ContainsKey("Not On US Check") ? transactions["Not On US Check"].ElementAtOrDefault(i) ?? "0" : "0",
        Taxes = transactions.ContainsKey("Taxes") ? transactions["Taxes"].ElementAtOrDefault(i) ?? "0" : "0",

      };
    }
    return data;
  }
}
