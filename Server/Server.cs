
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Text.Json;
using System.IO;
using System.ComponentModel;

public class Server
{
    private readonly int _port;

    public Server(int port)
    {
        _port = port;

        
    }


    public void Run() { 
 
        var server = new TcpListener(IPAddress.Loopback, _port); // IPv4 127.0.0.1 IPv6 ::1
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

            string validPath = "/api/category";
            if(!validPath.Contains("/api/category"))
            {
                var response = new Response
                {
                    Status = "4 Bad Request"
                };
                WriteToStream(stream, ToJson(response));
                return;
            }

          
            if (request == null)
            {
                var response = new Response
                {
                    Status = "invalid request"
                };
                WriteToStream(stream, ToJson(response));
                return;
            }

            
            if (request.Date == null)
            {
                var response = new Response
                {
                    Status = "missing date"
                };
                WriteToStream(stream, ToJson(response));
                return;
            }
            if (!ValidUnixTime(request.Date.ToString()))
            {
                var response = new Response
                {
                    Status = "illegal date"
                };
                WriteToStream(stream, ToJson(response));
                return;
            }

           
            string[] validMethods = { "create", "read", "update", "delete", "echo" };
            if (!validMethods.Contains(request.Method))
            {
                var response = new Response
                {
                    Status = "illegal method"
                };
                WriteToStream(stream, ToJson(response));
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);  
        }
        finally
        {
            
            stream.Close();
            client.Close();
        }
    }


    private bool ValidUnixTime(string dateString)
    {
        long unixTime;
        return long.TryParse(dateString, out unixTime) && unixTime >= 0;
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
        return JsonSerializer.Deserialize<Request>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
