// Copyright © 2017 - MazyModz. Created by Dennis Andersson. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace WebSocketServer
{
    /// <summary>
    /// Holds data for a encoded message frame
    /// </summary>
    public struct SFrameMaskData
    {
        public int DataLength, KeyIndex, TotalLenght;
        public EOpcodeType Opcode;

        public SFrameMaskData(int DataLength, int KeyIndex, int TotalLenght, EOpcodeType Opcode)
        {
            this.DataLength = DataLength;
            this.KeyIndex = KeyIndex;
            this.TotalLenght = TotalLenght;
            this.Opcode = Opcode;
        }
    }

    /// <summary>
    /// Enum for opcode types
    /// </summary>
    public enum EOpcodeType
    {
        /* Denotes a continuation code */
        Fragment = 0,

        /* Denotes a text code */
        Text = 1,

        /* Denotes a binary code */
        Binary = 2,

        /* Denotes a closed connection */
        ClosedConnection = 8,

        /* Denotes a ping*/
        Ping = 9,

        /* Denotes a pong */
        Pong = 10
    }

    /// <summary>
    /// Helper methods for the Server and Client class
    /// </summary>
    public static class Helpers
    {
        /// <summary>Gets data for a encoded websocket frame message</summary>
        /// <param name="Data">The data to get the info from</param>
        /// <returns>The frame data</returns>
        public static SFrameMaskData GetFrameData(byte[] Data)
        {
            // Get the opcode of the frame
            int opcode = Data[0] - 128;

            // If the length of the message is in the 2 first indexes
            if (Data[1] - 128 <= 125)
            {
                int dataLength = (Data[1] - 128);
                return new SFrameMaskData(dataLength, 2, dataLength + 6, (EOpcodeType)opcode);
            }

            // If the length of the message is in the following two indexes
            if (Data[1] - 128 == 126)
            {
                // Combine the bytes to get the length
                int dataLength = BitConverter.ToInt16(new byte[] { Data[3], Data[2] }, 0);
                return new SFrameMaskData(dataLength, 4, dataLength + 8, (EOpcodeType)opcode);
            }

            // If the data length is in the following 8 indexes
            if (Data[1] - 128 == 127)
            {
                // Get the following 8 bytes to combine to get the data 
                byte[] combine = new byte[8];
                for (int i = 0; i < 8; i++) combine[i] = Data[i + 2];

                // Combine the bytes to get the length
                //int dataLength = (int)BitConverter.ToInt64(new byte[] { Data[9], Data[8], Data[7], Data[6], Data[5], Data[4], Data[3], Data[2] }, 0);
                int dataLength = (int)BitConverter.ToInt64(combine, 0);
                return new SFrameMaskData(dataLength, 10, dataLength + 14, (EOpcodeType)opcode);
            }

            // error
            return new SFrameMaskData(0, 0, 0, 0);
        }

        /// <summary>Gets the opcode of a frame</summary>
        /// <param name="Frame">The frame to get the opcode from</param>
        /// <returns>The opcode of the frame</returns>
        public static EOpcodeType GetFrameOpcode(byte[] Frame)
        {
            return (EOpcodeType)Frame[0] - 128;
        }

        /// <summary>Gets the decoded frame data from the given byte array</summary>
        /// <param name="Data">The byte array to decode</param>
        /// <returns>The decoded data</returns>
        public static string GetDataFromFrame(byte[] Data)
        {
            // Get the frame data
            SFrameMaskData frameData = GetFrameData(Data);

            // Get the decode frame key from the frame data
            byte[] decodeKey = new byte[4];
            for (int i = 0; i < 4; i++) decodeKey[i] = Data[frameData.KeyIndex + i];

            int dataIndex = frameData.KeyIndex + 4;
            int count = 0;

            // Decode the data using the key
            for (int i = dataIndex; i < frameData.TotalLenght; i++)
            {
                Data[i] = (byte)(Data[i] ^ decodeKey[count % 4]);
                count++;
            }

            // Return the decoded message 
            return Encoding.Default.GetString(Data, dataIndex, frameData.DataLength);
        }

        /// <summary>Checks if a byte array is valid</summary>
        /// <param name="Buffer">The byte array to check</param>
        /// <returns>'true' if the byte array is valid</returns>
        public static bool GetIsBufferValid(ref byte[] Buffer)
        {
            if (Buffer == null) return false;
            if (Buffer.Length <= 0) return false;

            return true;
        }

        /// <summary>Gets an encoded websocket frame to send to a client from a string</summary>
        /// <param name="Message">The message to encode into the frame</param>
        /// <param name="Opcode">The opcode of the frame</param>
        /// <returns>Byte array in form of a websocket frame</returns>
        public static byte[] GetFrameFromString(string Message, EOpcodeType Opcode = EOpcodeType.Text)
        {
            byte[] response;
            byte[] bytesRaw = Encoding.Default.GetBytes(Message);
            byte[] frame = new byte[10];

            int indexStartRawData = -1;
            int length = bytesRaw.Length;

            frame[0] = (byte)(128 + (int)Opcode);
            if (length <= 125)
            {
                frame[1] = (byte)length;
                indexStartRawData = 2;
            }
            else if (length >= 126 && length <= 65535)
            {
                frame[1] = (byte)126;
                frame[2] = (byte)((length >> 8) & 255);
                frame[3] = (byte)(length & 255);
                indexStartRawData = 4;
            }
            else
            {
                frame[1] = (byte)127;
                frame[2] = (byte)((length >> 56) & 255);
                frame[3] = (byte)((length >> 48) & 255);
                frame[4] = (byte)((length >> 40) & 255);
                frame[5] = (byte)((length >> 32) & 255);
                frame[6] = (byte)((length >> 24) & 255);
                frame[7] = (byte)((length >> 16) & 255);
                frame[8] = (byte)((length >> 8) & 255);
                frame[9] = (byte)(length & 255);

                indexStartRawData = 10;
            }

            response = new byte[indexStartRawData + length];

            int i, reponseIdx = 0;

            //Add the frame bytes to the reponse
            for (i = 0; i < indexStartRawData; i++)
            {
                response[reponseIdx] = frame[i];
                reponseIdx++;
            }

            //Add the data bytes to the response
            for (i = 0; i < length; i++)
            {
                response[reponseIdx] = bytesRaw[i];
                reponseIdx++;
            }

            return response;
        }

        /// <summary>Hash a request key with SHA1 to get the response key</summary>
        /// <param name="Key">The request key</param>
        /// <returns></returns>
        public static string HashKey(string Key)
        {
            const string handshakeKey = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string longKey = Key + handshakeKey;

            SHA1 sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(longKey));

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>Gets the http request string to send to the websocket client</summary>
        /// <param name="Key">The SHA1 hashed key to respond with</param>
        /// <returns></returns>
        public static string GetHandshakeResponse(string Key)
        {
            return string.Format("HTTP/1.1 101 Switching Protocols\r\nUpgrade: WebSocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {0}\r\n\r\n", Key);
        }

        /// <summary>Gets the WebSocket handshake updgrade key from the http request</summary>
        /// <param name="HttpRequest">The http request string to get the key from</param>
        /// <returns></returns>
        public static string GetHandshakeRequestKey(string HttpRequest)
        {
            int keyStart = HttpRequest.IndexOf("Sec-WebSocket-Key: ") + 19;
            string key = null;

            for (int i = keyStart; i < (keyStart + 24); i++)
            {
                key += HttpRequest[i];
            }
            return key;
        }

        /// <summary>Creates a random guid with a prefix</summary>
        /// <param name="Prefix">The prefix of the id; null = no prefix</param>
        /// <param name="Length">The length of the id to generate</param>
        /// <returns>The random guid. Ex. Prefix-XXXXXXXXXXXXXXXX</returns>
        public static string CreateGuid(string Prefix, int Length = 16)
        {
            string final = null;
            string ids = "0123456789abcdefghijklmnopqrstuvwxyz";

            Random random = new Random();

            // Loop and get a random index in the ids and append to id 
            for (short i = 0; i < Length; i++) final += ids[random.Next(0, ids.Length)];

            // Return the guid without a prefix
            if (Prefix == null) return final;

            // Return the guid with a prefix
            return string.Format("{0}-{1}", Prefix, final);
        }
    }
}
