using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;

namespace WebSocketServer
{
    class Program
    {
        // Example application for the websocker server
        static void Main(string[] args)
        {
            // Create a listen server on localhost with port 80
            Server server = new Server(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 80));

            /*
             * Bind required events for the server
             */

            server.OnClientConnected += (object sender, OnClientConnectedHandler e) => 
            {
                Console.WriteLine("Client with GUID: {0} Connected!", e.GetClient().GetGuid());
            };

            server.OnClientDisconnected += (object sender, OnClientDisconnectedHandler e) =>
            {
                Console.WriteLine("Client {0} Disconnected", e.GetClient().GetGuid());
            };

            server.OnMessageReceived += (object sender, OnMessageReceivedHandler e) =>
            {
                Console.WriteLine("Received Message: '{1}' from client: {0}", e.GetClient().GetGuid(), e.GetMessage());
            };

            server.OnSendMessage += (object sender, OnSendMessageHandler e) =>
            {
                Console.WriteLine("Sent message: '{0}' to client {1}", e.GetMessage(), e.GetClient().GetGuid());
            };

            // Close the application only when the close button is clicked
            Process.GetCurrentProcess().WaitForExit();
        }
    }
}
