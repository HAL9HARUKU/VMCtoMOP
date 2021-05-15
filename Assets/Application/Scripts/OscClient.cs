using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using uOSC;

public class OscClient : MonoBehaviour
{
    private const int BufferSize = 8192;
    private const int MaxQueueSize = 100;
    
    private UdpClient udp_;
    private Queue<object> messages_ = new Queue<object>();
    private object lockObject_ = new object();

    public void Run(string address, int port)
    {
        Stop();
        
        try
        {
            udp_ = new UdpClient(address, port);
        }
        catch(System.Exception)
        {
            Stop();
        }
    }
    public void Stop()
    {
        udp_?.Close();
        udp_ = null;
    }

    private void Update()
    {
        if (udp_ == null)
        {
            messages_.Clear();
            return;
        }
        
        try
        {
            while (messages_.Count > 0)
            {
                object message;
                lock (lockObject_)
                {
                    message = messages_.Dequeue();
                }

                using (var stream = new MemoryStream(BufferSize))
                {
                    if (message is Message)
                    {
                        ((Message)message).Write(stream);
                    }
                    else if (message is Bundle)
                    {
                        ((Bundle)message).Write(stream);
                    }
                    else
                    {
                        return;
                    }
                    udp_.SendAsync(Util.GetBuffer(stream), (int)stream.Position);
                }
            }
        }
        catch(Exception)
        {
            Stop();
        }
    }

    private void Add(object data)
    {
        lock (lockObject_)
        {
            messages_.Enqueue(data);

            while (messages_.Count > MaxQueueSize)
            {
                messages_.Dequeue();
            }
        }
    }

    public void Send(string address, params object[] values)
    {
        Send(new Message() 
        {
            address = address,
            values = values
        });
    }

    public void Send(Message message)
    {
        Add(message);
    }

    public void Send(Bundle bundle)
    {
        Add(bundle);
    }
}
