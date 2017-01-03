// Copyright © 2017 - MazyModz. Created by Dennis Andersson. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace WebSocketServer
{
    ///<summary>
    /// Object for all connectecd clients
    /// </summary>
    public partial class Client
    {

        #region Fields

        ///<summary>The socket of the connected client</summary>
        private Socket _socket;

        ///<summary>The guid of the connected client</summary>
        private string _guid;

        /// <summary>The server that the client is connected to</summary>
        private Server _server;

        /// <summary>If the server has sent a ping to the client and is waiting for a pong</summary>
        private bool _bIsWaitingForPong;

        #endregion

        #region Class Events

        /// <summary>Create a new object for a connected client</summary>
        /// <param name="Server">The server object instance that the client is connected to</param>
        /// <param name="Socket">The socket of the connected client</param>
        public Client(Server Server, Socket Socket)
        {
            this._server = Server;
            this._socket = Socket;
            this._guid = Helpers.CreateGuid("client");

            // Start to detect incomming messages 
            GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, messageCallback, null);
        }

        #endregion

        #region Field Getters

        /// <summary>Gets the guid of the connected client</summary>
        /// <returns>The GUID of the client</returns>
        public string GetGuid()
        {
            return _guid;
        }

        ///<summary>Gets the socket of the connected client</summary>
        ///<returns>The socket of the client</return>
        public Socket GetSocket()
        {
            return _socket;
        }

        /// <summary>The socket that this client is connected to</summary>
        /// <returns>Listen socket</returns>
        public Server GetServer()
        {
            return _server;
        }

        /// <summary>Gets if the server is waiting for a pong response</summary>
        /// <returns>If the server is waiting for a pong response</returns>
        public bool GetIsWaitingForPong()
        {
            return _bIsWaitingForPong;
        }

        #endregion

        #region Field Setters

        /// <summary>Sets if the server is waiting for a pong response</summary>
        /// <param name="bIsWaitingForPong">If the server is waiting for a pong response</param>
        public void SetIsWaitingForPong(bool bIsWaitingForPong)
        {
            _bIsWaitingForPong = bIsWaitingForPong;
        }

        #endregion

        #region Methods

        /// <summary>Called when a message was received from the client</summary>
        private void messageCallback(IAsyncResult AsyncResult)
        {
            try
            {
                GetSocket().EndReceive(AsyncResult);

                // Read the incomming message 
                byte[] messageBuffer = new byte[8];
                int bytesReceived = GetSocket().Receive(messageBuffer);

                // Resize the byte array to remove whitespaces 
                if (bytesReceived < messageBuffer.Length) Array.Resize<byte>(ref messageBuffer, bytesReceived);

                // Get the opcode of the frame
                EOpcodeType opcode = Helpers.GetFrameOpcode(messageBuffer);

                // If the connection was closed
                if(opcode == EOpcodeType.ClosedConnection)
                {
                    GetServer().ClientDisconnect(this);
                    return;
                }

                // Pass the message to the server event to handle the logic
                GetServer().ReceiveMessage(this, Helpers.GetDataFromFrame(messageBuffer));

                // Start to receive messages again
                GetSocket().BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, messageCallback, null);

            }
            catch(Exception Exception)
            {
                GetSocket().Close();
                GetSocket().Dispose();
                GetServer().ClientDisconnect(this);
            }
        }

        #endregion

    }
}
