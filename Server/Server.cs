using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Server
{
    private readonly int _port;

    public Server(int port)
    {
        _port = port;
    }

    public void Run()
    {
        var server = new TcpListener(IPAddress.Loopback, _port);
        server.Start();

        Console.WriteLine($"Server started on port {_port}");

        while (true)
        {
            var client = server.AcceptTcpClient();
            Console.WriteLine("Client connected!!!");

            Task.Run(() => HandleClient(client));
        }
    }

    private void HandleClient(TcpClient client)
    {
        var stream = client.GetStream();
        try
        {
            string msg = ReadFromStream(stream);
            Console.WriteLine("Message from client: " + msg);

            // Check for empty or invalid messages
            if (string.IsNullOrWhiteSpace(msg) || msg == "{}")
            {
                var response = new Response
                {
                    Status = "missing method"
                };
                WriteToStream(stream, ToJson(response));
                return;
            }

            var request = FromJson(msg);
            var errors = new List<string>();

            // Validate request and collect errors
            if (request == null)
            {
                errors.Add("invalid request");
            }
            else
            {
                if (string.IsNullOrEmpty(request.Method))
                {
                    errors.Add("missing method");
                }
                else
                {
                    string[] validMethods = { "create", "read", "update", "delete", "echo" };
                    if (!validMethods.Contains(request.Method))
                    {
                        errors.Add("illegal method");
                    }
                }

                if (request.Method != "echo" && string.IsNullOrEmpty(request.Path))
                {
                    errors.Add("missing resource");
                }

                if (string.IsNullOrEmpty(request.Date))
                {
                    errors.Add("missing date");
                }
                else if (!ValidUnixTime(request.Date.ToString()))
                {
                    errors.Add("illegal date");
                }
            }
            
            if (errors.Count > 0)
            {
                var response = new Response
                {
                    Status = "4 " + string.Join(", ", errors)
                };
                WriteToStream(stream, ToJson(response));
                return;
            }

            // Handle specific valid requests, like checking the path for "read" or "update"
            if (request.Method == "read" && !request.Path.StartsWith("/api/category"))
            {
                var response = new Response
                {
                    Status = "4 Bad Request"
                };
                WriteToStream(stream, ToJson(response));
                return;
            }

        }
        catch (Exception e)
        {
            Console.WriteLine("Error handling client: " + e.Message);
        }
        finally
        {
            stream.Close();
            client.Close();
        }
    }

    private bool ValidUnixTime(string dateString)
    {
        return long.TryParse(dateString, out long unixTime) && unixTime >= 0;
    }

    private string ReadFromStream(NetworkStream stream)
    {
        var buffer = new byte[1024];
        var readCount = stream.Read(buffer);
        return Encoding.UTF8.GetString(buffer, 0, readCount);
    }

    private void WriteToStream(NetworkStream stream, string msg)
    {
        var buffer = Encoding.UTF8.GetBytes(msg);
        stream.Write(buffer);
    }

    public static string ToJson(Response response)
    {
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public static Request? FromJson(string element)
    {
        try
        {
            return JsonSerializer.Deserialize<Request>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
