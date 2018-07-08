using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.IO;

public class Server : MonoBehaviour
{
    [SerializeField]
    private List<ServerClient> clients;
    private List<ServerClient> disconnectList;

    public int port = 6321;
    private TcpListener server;
    private bool serverStarted;

    private void Start()
    {
        clients = new List<ServerClient>();
        disconnectList = new List<ServerClient>();

        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Debug.Log("Server has been started on port : "+port.ToString());
            serverStarted = true;
            StartListenening();
        }
        catch(Exception e)
        {
            serverStarted = false;
            Debug.Log("Socket error = "+e.Message);
        }
    }

    private void Update()
    {
        if (!serverStarted)
            return;

        foreach(ServerClient c in clients)
        {
            //Is the client still connected
            if(!IsConnected(c.tcp))
            {
                c.tcp.Close();
                disconnectList.Add(c);
                continue;
            }
            //Check for messages from the client
            else
            {
                NetworkStream s = c.tcp.GetStream();
                if(s.DataAvailable)
                {
                    StreamReader reader = new StreamReader(s, true);
                    string data = reader.ReadLine();

                    if(data != null)
                    {
                        OnIncomingData(c, data);
                    }
                }
            }
        }
    }

    private void OnIncomingData(ServerClient c, string data)
    {
        Broadcast(c.clientName+" : "+data, clients);
    }

    private bool IsConnected(TcpClient tcp)
    {
        try
        {
            if(tcp != null && tcp.Client != null && tcp.Client.Connected)
            {
                if(tcp.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(tcp.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        catch(Exception e)
        {
            return false;
        }
    }

    private void StartListenening()
    {
        //Debug.Log("Start Listening");
        server.BeginAcceptTcpClient(AcceptTCPClient, server);
    }

    private void AcceptTCPClient(IAsyncResult ar)
    {
        Debug.Log("AcceptTCPClient");

        TcpListener listener = (TcpListener)ar.AsyncState;
        clients.Add(new ServerClient(listener.EndAcceptTcpClient(ar)));
        StartListenening();

        //Send Message to everyone, say someone has connected
        Broadcast(clients[clients.Count - 1].clientName + " has connected", clients);
    }

    private void Broadcast(string data, List<ServerClient> cl)
    {
        foreach(ServerClient c in cl)
        {
            try
            {
                StreamWriter writer = new StreamWriter(c.tcp.GetStream());
                writer.WriteLine(data);
                writer.Flush();
            }
            catch(Exception e)
            {
                Debug.Log("Write Error : " + e.Message + " to client : " + c.clientName);
            }
        }
    }
}

[Serializable]
public class ServerClient
{
    public TcpClient tcp;
    public string clientName;

    public ServerClient(TcpClient clientSocket)
    {
        clientName = "Guest";
        tcp = clientSocket;
    }
}
