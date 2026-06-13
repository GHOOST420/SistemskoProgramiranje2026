using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DrugiProjekatSisProg
{
    internal class Server
    {
        private static readonly HttpListener listener = new();
        private static readonly Cache cache = new();
        private static CancellationTokenSource? _cts;

      
        private const string InternalApiUrl = "http://localhost";
        private const int InternalApiPort = 5080;

        

        public static void StartAsync()
        {
            _cts = new CancellationTokenSource();
            Logger.Start(_cts.Token);

            var apiUrl = $"{InternalApiUrl}:{InternalApiPort}/";
            listener.Prefixes.Add(apiUrl);
            listener.Start();

            Logger.Info($"Server started on {apiUrl}");
            Logger.Info("Press ENTER to stop.");

            _ = ListenAsync(_cts.Token);
        }

        private static async Task ListenAsync(CancellationToken token)
        {
            try
            {
                while (listener.IsListening && !token.IsCancellationRequested)
                {
                    var context = await listener.GetContextAsync();
                    _ = HandleRequestAsync(context);
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

        public static async Task StopAsync()
        {
            try
            {
                Logger.Info("Server is shutting down...");
                if (_cts is not null && !_cts.IsCancellationRequested)
                    _cts.Cancel();

                if (listener.IsListening)
                {
                    listener.Stop();
                    listener.Close();
                }

                
                await Logger.StopAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while stopping server: {ex.Message}");
            }
        }

        private static async Task HandleRequestAsync(HttpListenerContext context)
        {
            try
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
                    await ReturnTextAsync(context, "Only GET method is supported.", HttpStatusCode.MethodNotAllowed);
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
                    await ReturnTextAsync(context, "Nepotpun naziv fajla", HttpStatusCode.BadRequest);

                    return;
                }

                string cacheKey = fileName;


                string? response = await cache.GetOrAddAsync(cacheKey, () =>
                {

                 return Algoritam.StartAsync(fileName);//samo funkc 
                    

                });

        
                    await ReturnTextAsync(context, response, HttpStatusCode.OK);

            }
            catch (Exception ex)
            {
                Logger.Error($"Error handling request: {ex.Message}");
                await ReturnTextAsync(context, $"Server error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }


        private static async ValueTask ReturnTextAsync(HttpListenerContext context, string text, HttpStatusCode status)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var response = context.Response;
            response.ContentType = "text/plain; charset=utf-8";
            response.StatusCode = (int)status;
            AddCorsHeaders(response);

            await using var output = response.OutputStream;
            await output.WriteAsync(buffer.AsMemory());

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
