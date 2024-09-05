using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using TaskStatsServer.Extensions;
using TaskStatsServer.TaskStats;

class Program
{
    private static HttpListener? _listener = null;
    private static int? _port = null;
    private static string[]? _featuredProcessPatterns = null;
    private static List<Dictionary<string, string>>? _latestProcesses = null;
    private static string? _prefix = null;

    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: TaskStatsServer.exe <port> <featuredProcessPatterns>");
            return;
        }

        _port = int.Parse(args[0]);
        _featuredProcessPatterns = string.Join(" ", args.Slice(1)).Split(",");
        _prefix = $"http://127.0.0.1:{_port}/";
        _listener = new HttpListener();
        _listener.Prefixes.Add(_prefix);
        _listener.Start();

        var analyzerThread = new Thread(StartAnalyzer);
        analyzerThread.Start();
        HandleIncomingConnections().GetAwaiter().GetResult();
    }

    private static void StartAnalyzer()
    {
        if (_featuredProcessPatterns == null)
        {
            return;
        }

        var analyzer = new TaskmgrAnalyzer();
        analyzer.StartAnalyzer(_featuredProcessPatterns.ToList());
        while (true)
        {
            Thread.Sleep(millisecondsTimeout: 1000);
            _latestProcesses = analyzer.ExtractFeaturedProcessInfo();
        }
    }

    private static async Task HandleIncomingConnections()
    {
        if (_listener == null || _featuredProcessPatterns == null)
        {
            return;
        }

        Console.WriteLine($"""
            ============================================
             _____         _     ____  _        _       
            |_   _|_ _ ___| | __/ ___|| |_ __ _| |_ ___ 
              | |/ _` / __| |/ /\___ \| __/ _` | __/ __|
              | | (_| \__ \   <  ___) | || (_| | |_\__ \
              |_|\__,_|___/_|\_\|____/ \__\__,_|\__|___/

                       TaskStatsServer v0.0.1
            ============================================

              * Listening at {_prefix}
              * Featured Process Patterns: {string.Join(", ", _featuredProcessPatterns)}
            """);

        while (true)
        {
            HttpListenerContext context = await _listener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Serialize
            var jsonString = "[";
            foreach (var process in _latestProcesses ?? [])
            {
                var processJson = "{";
                foreach (var (key, value) in process)
                {
                    processJson += $"\"{key}\":\"{value}\",";
                }
                processJson = processJson.TrimEnd(',') + "}";
                jsonString += processJson + ",";
            }
            jsonString = jsonString.TrimEnd(',') + "]";

            byte[] data = Encoding.UTF8.GetBytes(jsonString);
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = data.LongLength;
            await response.OutputStream.WriteAsync(data, 0, data.Length);
            response.Close();
        }
    }
}
