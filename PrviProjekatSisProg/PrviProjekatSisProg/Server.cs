using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PrviProjekatSisProg
{
    internal class Server
    {
        private static HttpListener listener = new HttpListener();
        private const string InternalApiUrl = "http://localhost";
        private const int InternalApiPort = 5080;
        private static object Lock=new object();
        private static readonly Cache cache = new();

        public static void Start() 
        {
            var apiUrl = $"{InternalApiUrl}:{InternalApiPort}/";
            listener.Prefixes.Add(apiUrl);
            listener.Start();
            Logger.Info($"Server started on {apiUrl}");
            Logger.Info("Press ENTER to stop.");
            var listenerThread = new Thread(Listen)
            {
                IsBackground = true,
                Name = "HttpListenerThread"
            };
            listenerThread.Start();

        }

        private static void Listen() 
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
            }
            catch (HttpListenerException)
            {
                Logger.Info("Server stopped.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in listener: {ex.Message}");
            }
        }

        private static void HandleRequest(HttpListenerContext context) 
        {
            var request = context.Request;


            if (request.HttpMethod == "OPTIONS")
            {
                AddCorsHeaders(context.Response);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Close();
                return;
            }

            if (request.HttpMethod != "GET")
            {
                ReturnText(context, "Only GET method is supported.", HttpStatusCode.MethodNotAllowed);
                return;
            }

            if (request.RawUrl == "/favicon.ico")
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                context.Response.Close();
                return;
            }

            string fileName = Path.GetFileName(request.Url.AbsolutePath);
            Logger.Request($"Klijent trazi zahtev za fajl {fileName}");
            if (!Path.HasExtension(fileName))
            {
                ReturnText(context, "Nepotpun naziv fajla", HttpStatusCode.BadRequest);
               
                return;
            }
            
            string cacheKey = fileName;
            string? response = cache.GetOrAdd(cacheKey, () =>
             Algoritam.Start(fileName));

            ReturnText(context, response, HttpStatusCode.OK);



            }

        private static void ReturnJson(HttpListenerContext context, string json, HttpStatusCode status)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            var response = context.Response;
            response.ContentType = "application/json; charset=utf-8";
            response.StatusCode = (int)status;
            AddCorsHeaders(response);

            using var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);

            Logger.Request($"Request completed: {context.Request.RawUrl}, Status: {status}");
        }
        public static void Close()
        {
            try
            {
                if (listener.IsListening)
                {
                    listener.Stop();
                    listener.Close();
                }

                Logger.Info("Server successfully stopped.");
                Logger.Stop();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while stopping server: {ex.Message}");
            }
        }
       

        private static void ReturnText(HttpListenerContext context, string text, HttpStatusCode status)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var response = context.Response;
            response.ContentType = "text/plain; charset=utf-8";
            response.StatusCode = (int)status;
            AddCorsHeaders(response);

            using var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            bool imaBroj = text.Any(char.IsDigit);
            if (!imaBroj) { Logger.Error($"Response {status}: {text}"); }
            else
            {
                Logger.Info($"Response {status}: {text}");
            }
        }
        private static void AddCorsHeaders(HttpListenerResponse response)
        {
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
        }
    }
}
