using BestHTTP;

public static class HttpExtensions
{
    private const int OkStatusCode = 200;

    public static bool IsOk(this HTTPRequest request)
    {
        return request?.Response.StatusCode == OkStatusCode;
    }
}