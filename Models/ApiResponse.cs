using System.Collections.Generic;
using System.Text.Json.Serialization;

public class ApiResponse<T>
{
    [JsonPropertyName("exitcode")]
    public int Exitcode { get; set; }

    [JsonPropertyName("data")]
    public ApiDataWrapper<T> Data { get; set; }
}

public class ApiDataWrapper<T>
{
    [JsonPropertyName("nItems")]
    public int nItems { get; set; }

    [JsonPropertyName("nPages")]
    public int nPages { get; set; }

    [JsonPropertyName("data")]
    public List<T> Data { get; set; } // Chữ "Data" trong C# nhưng ánh xạ từ JSON "data"
}
