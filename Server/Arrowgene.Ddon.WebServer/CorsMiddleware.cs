using System.Threading.Tasks;
using Arrowgene.WebServer;
using Arrowgene.WebServer.Middleware;

public class CorsMiddleware : IWebMiddleware
{
    public async Task<WebResponse> Handle(WebRequest request, WebMiddlewareDelegate next)
    {
        WebResponse response;

        if (request.Method.ToString().ToUpper() == "OPTIONS")
        {
            response = new WebResponse();
            response.StatusCode = 200;
            await AddCorsHeaders(response);
            await response.WriteAsync("");
            return response;
        }

        response = await next(request);
        await AddCorsHeaders(response);
        return response;
    }

    private Task AddCorsHeaders(WebResponse response)
    {
        response.Header["Access-Control-Allow-Origin"] = "*";
        response.Header["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, HEAD, OPTIONS";
        response.Header["Access-Control-Allow-Headers"] = "Content-Type, Authorization";

        return Task.CompletedTask;
    }
}
