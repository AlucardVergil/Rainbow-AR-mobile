using System;
using Unity.WebRTC;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace SimpleDataChannelRainbow
{
    public class DataChannel
    {
        private DelegateOnMessage onMessage;
        private DelegateOnOpen onOpen;
        private DelegateOnClose onClose;
        private DelegateOnError onError;
        private SimpleDataChannelService service;
        private RTCDataChannel instance;
        private ConnectionState cnx;
        internal bool connected = false;
        private bool closed = false;

        internal static DataChannel Wrap(RTCDataChannel c, SimpleDataChannelService s, ConnectionState cnx)
        {
            DataChannel result = new DataChannel() { instance = c, service = s, cnx = cnx };
            c.OnClose = result.OnInstanceClosed;
            c.OnOpen = result.OnInstanceOpen;
            c.OnError = result.OnInstanceError;
            c.OnMessage = result.OnInstanceMessage;            
            return result;
        }

        internal void OnInstanceOpen()
        {
            connected  = true; 
            if( onOpen != null ) { onOpen(); }
            // TODO tell the service 

        }
        internal void OnInstanceClosed()
        {
            closed = true;
            Debug.Log("DC WAS CLOSED (event on close)");
            // TODO tell the service 
            service.CloseDataChannel(this.cnx);
            instance = null;
            if( onClose != null ) {
                Debug.Log("call CLOSE ");
                onClose(); 
            }
        }

        internal void OnInstanceError(RTCError error)
        {
            closed = true;
            Debug.Log("DC Received Error " + error );
            // TODO tell the service 
            service.CloseDataChannel(this.cnx);
            instance = null;
            if (onClose != null)
            {
                Debug.Log("call CLOSE ");
                onClose();
            }
            if( onError != null )
            {
                onError(error);
            }
        }
        public void OnInstanceMessage(byte[] bytes)
        {
            if( onMessage != null ) {
                // Debug.Log($"ONMESSAGE length = {bytes.Length}");
                onMessage(bytes); 
            }
        }

        public DelegateOnMessage OnMessage
        {
            get { return onMessage; }
            set
            {
                onMessage = value;
            }
        }

        

        public DelegateOnOpen OnOpen
        {
            get { return onOpen; }
            set
            {
                onOpen = value;
            }
        }
         
        public DelegateOnClose OnClose
        {
            get { return onClose; }
            set
            {
                onClose = value;
            }
        }

        public DelegateOnError OnError
        {
            get { return onError; }
            set
            {
                onError = value;
            }
        }

        public int Id => instance.Id;

        public string Label => instance.Label;

        public string Protocol => instance.Protocol;
        public ushort MaxRetransmits => instance.MaxRetransmits;

        public ushort MaxRetransmitTime => instance.MaxRetransmitTime;

        public bool Ordered => instance.Ordered;

        public ulong BufferedAmount => instance.BufferedAmount;
        public bool Negotiated => instance.Negotiated;

        public RTCDataChannelState ReadyState => instance.ReadyState;
         
        public void Dispose()
        {
            // TODO tell the service 
            if (instance != null)
                instance.Dispose();
            instance = null;
            service = null;
            cnx = null;

        }
         
        public void Send(string msg)
        {
            if (closed)
            {
                throw new Exception("Data Channel closed");
            }
            else if (!connected)
            {
                throw new Exception("Data Channel not connected");
            }
            else
                instance.Send(msg);
        }
         
        public void Send(byte[] msg)
        {         
            if (closed)
            {
                throw new Exception("Data Channel closed");
            }
            else if (!connected)
            {
                throw new Exception("Data Channel not connected");
            }
            else
                instance.Send(msg);
            ulong buffered = instance.BufferedAmount;
            if( buffered != 0 )
            Debug.Log($"BUFFERED: {buffered}");
        } 
        public unsafe void Send<T>(NativeArray<T> msg)
            where T : struct
        {
            if (closed)
            {
                throw new Exception("Data Channel closed");
            }
            else if (!connected)
            {
                throw new Exception("Data Channel not connected");
            }
            else
                instance.Send(msg);
        } 
        public unsafe void Send<T>(NativeSlice<T> msg)
            where T : struct
        {
            if (closed)
            {
                throw new Exception("Data Channel closed");
            }
            else if (!connected)
            {
                throw new Exception("Data Channel not connected");
            }
            else
                instance.Send(msg);
        }

#if UNITY_2020_1_OR_NEWER // ReadOnly support was introduced in 2020.1
         
        public unsafe void Send<T>(NativeArray<T>.ReadOnly msg)
            where T : struct
        {
            if (closed)
            {
                throw new Exception("Data Channel closed");
            }
            else if (!connected)
            {
                throw new Exception("Data Channel not connected");
            }
            else
                instance.Send(msg);
        }
#endif
         
        public unsafe void Send(void* msgPtr, int length)
        {
            if(closed)
            {
                throw new Exception("Data Channel closed");
            }
            else if (!connected)
            {
                throw new Exception("Data Channel not connected");
            }
            else
                instance.Send(msgPtr, length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgPtr"></param>
        /// <param name="length"></param>
        public void Send(IntPtr msgPtr, int length)
        {
            if (closed )
            {
                throw new Exception("Data Channel closed");
            }
            else if (!connected) {
                throw new Exception("Data Channel not connected");
            }
            else
                instance.Send(msgPtr, length);
            
                
        }

        /// <summary>
        /// 
        /// </summary>
        public void Close()
        {
            if (instance != null)
            {
                // TODO tell the service
                service.CloseDataChannel(this.cnx);
                //instance.Close();
                //instance = null;
            }
        }
    }
}