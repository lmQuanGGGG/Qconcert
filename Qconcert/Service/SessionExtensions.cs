using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

public static class SessionExtensions
{
    public static void SetObjectAsJson(this ISession session, string key, object value)
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        session.SetString(key, JsonConvert.SerializeObject(value, settings));
    }

    public static T GetObjectFromJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
    }
}
