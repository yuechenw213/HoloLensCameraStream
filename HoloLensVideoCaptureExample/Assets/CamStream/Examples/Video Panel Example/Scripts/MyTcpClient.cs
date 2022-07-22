using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

#if !UNITY_EDITOR
using System.Threading.Tasks;
#endif

public class MyTcpClient : MonoBehaviour
{

#if !UNITY_EDITOR
    private bool _useUWP = true;
    private Windows.Networking.Sockets.StreamSocket socket;
    private Task exchangeTask;
#endif

#if UNITY_EDITOR
    private bool _useUWP = false;
    System.Net.Sockets.TcpClient client;
    System.Net.Sockets.NetworkStream stream;
    private Thread exchangeThread;
#endif

    private Byte[] bytes = new Byte[256];
    private StreamWriter writer;
    private StreamReader reader;

    public void Start()
    {
        //Server ip address and port
        Connect("18.27.123.102", "50001");
    }



    public void Connect(string host, string port)
    {
        if (_useUWP)
        {
            ConnectUWP(host, port);
        }
        else
        {
            ConnectUnity(host, port);
        }
    }



#if UNITY_EDITOR
    private void ConnectUWP(string host, string port)
#else
    private async void ConnectUWP(string host, string port)
#endif
    {
#if UNITY_EDITOR
        errorStatus = "UWP TCP client used in Unity!";
#else
        try
        {
            if (exchangeTask != null) StopExchange();
        
            socket = new Windows.Networking.Sockets.StreamSocket();
            Windows.Networking.HostName serverHost = new Windows.Networking.HostName(host);
            await socket.ConnectAsync(serverHost, port);
        
            Stream streamOut = socket.OutputStream.AsStreamForWrite();
            writer = new StreamWriter(streamOut) { AutoFlush = true };
        
            Stream streamIn = socket.InputStream.AsStreamForRead();
            reader = new StreamReader(streamIn);

            RestartExchange();
            successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private void ConnectUnity(string host, string port)
    {
#if !UNITY_EDITOR
        errorStatus = "Unity TCP client used in UWP!";
#else
        try
        {
            if (exchangeThread != null) StopExchange();

            client = new System.Net.Sockets.TcpClient(host, Int32.Parse(port));
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream) { AutoFlush = true };

            RestartExchange();
            successStatus = "Connected!";
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }
#endif
    }

    private bool exchanging = false;
    private bool exchangeStopRequested = false;
    private string lastPacket = null;

    private string errorStatus = null;
    private string warningStatus = null;
    private string successStatus = null;
    private string unknownStatus = null;

    public void RestartExchange()
    {
#if UNITY_EDITOR
        if (exchangeThread != null) StopExchange();
        exchangeStopRequested = false;
        exchangeThread = new System.Threading.Thread(ExchangePackets);
        exchangeThread.Start();
#else
        if (exchangeTask != null) StopExchange();
        exchangeStopRequested = false;
        exchangeTask = Task.Run(() => ExchangePackets());
#endif
    }

    public void Update()
    {
        //if (lastPacket != null)
        //{
        //    ReportDataToTrackingManager(lastPacket);
        //}

        if (errorStatus != null)
        {
            Debug.Log(errorStatus);
            errorStatus = null;
        }
        if (warningStatus != null)
        {
            Debug.Log(warningStatus);
            warningStatus = null;
        }
        if (successStatus != null)
        {
            Debug.Log(successStatus);
            successStatus = null;
        }
        if (unknownStatus != null)
        {
            Debug.Log(unknownStatus);
            unknownStatus = null;
        }
    }

    public void SendMessage2(string message)
    {
        writer.Write(message);
        Debug.Log("exchange once\n");
    }

    private bool queueempty = true;

    public void SendTexture(Texture2D texture)
    {
        if(!queueempty)
        {
            return;
        }

        queueempty = false;
        writer.Write("i");
        Debug.Log("width: " + texture.width + "  height: " + texture.height);
        byte[] bytes = texture.EncodeToJPG();
        if(bytes.Length > 872196)
        {
            return;
        }
        // Debug.Log(BitConverter.GetBytes(texture.width).Length);
        writer.BaseStream.Write(BitConverter.GetBytes(texture.width),0,4);
        writer.BaseStream.Write(BitConverter.GetBytes(texture.height), 0, 4);
       
        writer.BaseStream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
        Debug.Log(bytes.Length);
        writer.BaseStream.Write(bytes, 0, bytes.Length);
        Debug.Log("sent a texture");
        queueempty = true;
    }

    public void ExchangePackets()
    {
        bool exchanged = false;
        while (!exchangeStopRequested)
        {
            if (writer == null || reader == null) continue;
            exchanging = true;

            writer.Write("a~~~exchange once\n");
            Debug.Log("Sent data!");
            string received = null;

#if UNITY_EDITOR
            byte[] bytes = new byte[client.SendBufferSize];
            int recv = 0;
            while (true)
            {
                recv = stream.Read(bytes,0,1);
                if(bytes[0] == 'a') {
                
                    recv = stream.Read(bytes, 0, client.SendBufferSize);
                    received += Encoding.UTF8.GetString(bytes, 0, recv);
                    if (received.EndsWith("\n")) break;
                }else if(bytes[0] == 'r') {
                    byte[] countbytes = new byte[4];
                    stream.Read(countbytes, 0, 4);
                    int count = BitConverter.ToInt32(countbytes, 0);
                    for(int i = 0; i < count; i++) {
                        int[] xywh = new int[4];
                        byte[] intbuffer = new byte[4];
                        for(int j = 0; j < 4; j++) {
                            stream.Read(intbuffer, 0, 4);
                            xywh[j] = BitConverter.ToInt32(intbuffer, 0);
                            //Debug.Log(xywh[j]);
                        }
                        stream.Read(intbuffer, 0, 4);
                        int strlen = BitConverter.ToInt32(intbuffer, 0);
                        byte[] strbuffer = new byte[strlen];
                        stream.Read(strbuffer, 0, strlen);
                        string label = Encoding.UTF8.GetString(strbuffer, 0, strlen);
                        Debug.Log(label);
                    }

                }
            }
            lastPacket = received;
            Debug.Log("Read data: " + received);

#else
            int c = reader.Read();
            if(c == 'a')
            {
                received = reader.ReadLine();
                lastPacket = received;
                Debug.Log("Read data: " + received);

            }
            
#endif



            exchanging = false;
        }
    }

    private void ReportDataToTrackingManager(string data)
    {
        if (data == null)
        {
            Debug.Log("Received a frame but data was null");
            return;
        }

        var parts = data.Split(';');
        foreach (var part in parts)
        {
            ReportStringToTrackingManager(part);
        }
    }

    private void ReportStringToTrackingManager(string rigidBodyString)
    {
        var parts = rigidBodyString.Split(':');
        var positionData = parts[1].Split(',');
        var rotationData = parts[2].Split(',');

        int id = Int32.Parse(parts[0]);
        float x = float.Parse(positionData[0]);
        float y = float.Parse(positionData[1]);
        float z = float.Parse(positionData[2]);
        float qx = float.Parse(rotationData[0]);
        float qy = float.Parse(rotationData[1]);
        float qz = float.Parse(rotationData[2]);
        float qw = float.Parse(rotationData[3]);

        Vector3 position = new Vector3(x, y, z);
        Quaternion rotation = new Quaternion(qx, qy, qz, qw);


    }

    public void StopExchange()
    {
        exchangeStopRequested = true;
        Debug.Log("Stop exchange");
#if UNITY_EDITOR
        if (exchangeThread != null)
        {
            
            exchangeThread.Abort();
            stream.Close();
            client.Close();
            writer.Close();
            reader.Close();

            stream = null;
            exchangeThread = null;
            
        }
#else
        if (exchangeTask != null) {
            exchangeTask.Wait();
            socket.Dispose();
            writer.Dispose();
            reader.Dispose();

            socket = null;
            exchangeTask = null;
        }
#endif
        writer = null;
        reader = null;
    }

    public void OnDestroy()
    {
        StopExchange();

    }

}