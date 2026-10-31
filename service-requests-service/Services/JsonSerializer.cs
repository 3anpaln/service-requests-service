using System.Text;
using Newtonsoft.Json;

namespace service_requests_service.Services;

public class JsonSerializer
{
    public static byte[] JsonSerializeMessage<TMessage>(TMessage message)
    {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
    }

    public static TMessage JsonDeserializeMessage<TMessage>(ReadOnlyMemory<byte> encodedBytes)
    {
        //Need more information about an error
        return JsonConvert.DeserializeObject<TMessage>(Encoding.UTF8.GetString(encodedBytes.ToArray())) ?? throw new InvalidOperationException();
    }
}