using Confluent.Kafka;
using System.Text.Json;
using System.Numerics;

const string topic = "oracle_mobile.LOINH_THANHTOAN";
const string bootstrapServers = "my-cluster-kafka-bootstrap.kafka:9092";
const string apiEndpoint = "http://guithongbao-service.default.svc.cluster.local:5000/GUITHONGBAOKHANG";

var config = new ConsumerConfig
{
    GroupId = "thanhtoan-consumer-dotnet",
    BootstrapServers = bootstrapServers,
    AutoOffsetReset = AutoOffsetReset.Earliest,
};

using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
using var httpClient = new HttpClient();

consumer.Subscribe(topic);
Console.WriteLine($"📡 Listening to topic {topic}...");

try
{
    while (true)
    {
        var cr = consumer.Consume();
        var jsonDoc = JsonDocument.Parse(cr.Message.Value);
        var payload = jsonDoc.RootElement.GetProperty("payload");

        if (payload.TryGetProperty("__deleted", out var deleted) && deleted.GetString() == "true")
            continue;

        // ID (VariableScaleDecimal)
        var idBase64 = payload.GetProperty("ID").GetProperty("value").GetString();
        var idScale = payload.GetProperty("ID").GetProperty("scale").GetInt32();
        var id = DecodeDecimalFromBase64(idBase64, idScale);

        // MAKH
        var makh = payload.GetProperty("MAKH").GetString();

        // SOTIEN (Decimal with scale=2)
        var sotienBase64 = payload.GetProperty("SOTIEN").GetString();
        var sotien = DecodeDecimalFromBase64(sotienBase64, 2);

        // NGAYTT
        var timestamp = payload.GetProperty("NGAYTT").GetInt64();
        var ngaytt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("yyyy-MM-dd HH:mm:ss");

        var sendData = new
        {
            id,
            makh,
            sotien,
            ngaytt
        };

        Console.WriteLine($"✅ Thành công: {JsonSerializer.Serialize(sendData)}");

        // Nếu cần gửi HTTP:
        // await httpClient.PostAsJsonAsync(apiEndpoint, sendData);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Lỗi: {ex.Message}");
}
finally
{
    consumer.Close();
}

static decimal DecodeDecimalFromBase64(string base64, int scale)
{
    if (string.IsNullOrWhiteSpace(base64))
        throw new ArgumentException("Base64 input is null or empty.");

    var bytes = Convert.FromBase64String(base64);

    // Đảm bảo số dương và không bị lỗi bit dấu
    var bigInt = new BigInteger(bytes.Reverse().Concat(new byte[] { 0x00 }).ToArray());

    return (decimal)bigInt / (decimal)Math.Pow(10, scale);
}