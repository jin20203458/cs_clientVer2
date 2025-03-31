using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Program
{
    static void Main()
    {
        Console.WriteLine("Decide your name (only in English)");
        string name = Console.ReadLine();

        Socket sock = ConnectToServer("127.0.0.1", 5000);
        if (sock == null) return;

        Player localPlayer = new Player(sock, name);
        GameWorld gameWorld = new GameWorld();
        gameWorld.SetLocalPlayer(localPlayer);


        // start
        float x = 0, y = 0;
        localPlayer.Init();

        Thread recvThread = new Thread(() => ReceiveData(gameWorld));  // 수신 스레드
        recvThread.Start();

        // update
        while (true)
        {
            localPlayer.UpdatePosition(++x, ++y);
            localPlayer.SendPlayerData();
            Thread.Sleep(1000);  // 데이터 송신 간격을 1초로 설정
        }

        sock.Close();
    }

    static void ReceiveData(GameWorld gameWorld)
    {
        Player localPlayer = gameWorld.GetLocalPlayer();
        Packet recvPacket = new Packet();

        while (true)
        {
            if (!localPlayer.ReceivePlayerData(out recvPacket))
            {
                Console.WriteLine("Error receiving data from server");
                break;
            }
            else
            {
                var recvHeader = recvPacket.Header;
                var dataType = recvHeader.Type;

                if (dataType == PacketType.WorldUpdate)
                {
                    gameWorld.SyncWorldData(recvPacket);
                }
                else
                {
                    Console.WriteLine("Invalid packet type received");
                }
            }
        }
    }

    static Socket ConnectToServer(string ip, int port)
    {
        try
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(new IPEndPoint(IPAddress.Parse(ip), port));

            Console.WriteLine($"Connected to server: {ip}:{port}");
            return sock;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to server: {ex.Message}");
            return null;
        }
    }
}