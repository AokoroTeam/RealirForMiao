using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Realit.Builder.Miao.Pipes
{
    public class RealitPipeClient : MonoBehaviour
    {
        NamedPipeClientStream realitPipe;

        public void AddMessageToQueue(string message)
        {
            Debug.Log(message);
            if (realitPipe.IsConnected)
            {
                StreamString ss = new StreamString(realitPipe);
                ss.WriteString(message);
            }
        }

        public void Awake()
        {
            realitPipe = new NamedPipeClientStream(".", "RealitPipe", PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
            TryConnect();
        }

        public void TryConnect()
        {
#if !UNITY_EDITOR
            try
            {
                realitPipe.Connect(6000);
            }
            catch(TimeoutException e)
            {
                Debug.Log("Timeout");
            }
#endif
        }

        private void OnApplicationQuit()
        {
            realitPipe.Close();
        }
        public class StreamString
        {
            private Stream ioStream;
            private UnicodeEncoding streamEncoding;

            public StreamString(Stream ioStream)
            {
                this.ioStream = ioStream;
                streamEncoding = new UnicodeEncoding();
            }

            public string ReadString()
            {
                int len = 0;

                len = ioStream.ReadByte() * 256;
                len += ioStream.ReadByte();
                byte[] inBuffer = new byte[len];
                ioStream.Read(inBuffer, 0, len);

                return streamEncoding.GetString(inBuffer);
            }

            public int WriteString(string outString)
            {
                byte[] outBuffer = streamEncoding.GetBytes(outString);
                int len = outBuffer.Length;
                if (len > UInt16.MaxValue)
                {
                    len = (int)UInt16.MaxValue;
                }
                ioStream.WriteByte((byte)(len / 256));
                ioStream.WriteByte((byte)(len & 255));
                ioStream.Write(outBuffer, 0, len);
                ioStream.Flush();

                return outBuffer.Length + 2;
            }
        }
    }
}
