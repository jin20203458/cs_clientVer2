﻿using System;
using System.Collections.Generic;
using System.Threading;

public class GameWorld
{
    private Player localPlayer;
    private Dictionary<string, RemotePlayer> remotePlayers;
    private readonly object worldMutex;

    public GameWorld()
    {
        remotePlayers = new Dictionary<string, RemotePlayer>();
        worldMutex = new object();
    }

    public void SetLocalPlayer(Player player)=> localPlayer = player;
    public Player GetLocalPlayer() => localPlayer;

    public void SyncWorldData(Packet packet)
    {
        lock (worldMutex)
        {
            int playerCount = packet.ReadInt();
            Console.WriteLine($"Player count: {playerCount}");
            var updatedPlayers = new Dictionary<string, bool>();
            string myPlayerName = localPlayer.GetName();

            for (int i = 0; i < playerCount; ++i)
            {
                string playerName = packet.ReadString();
                float posX = packet.ReadFloat();
                float posY = packet.ReadFloat();

                if (playerName == myPlayerName || playerName == "UninitPlayer")continue;

                Console.WriteLine($"Player {playerName} : move x{posX}, y{posY}");

                if (remotePlayers.TryGetValue(playerName, out var remotePlayer))
                {
                    // 기존 목록 최신화
                    remotePlayer.UpdatePosition(posX, posY);
                }
                else
                {
                    // 신규 플레이어 추가
                    var newPlayer = new RemotePlayer(playerName);
                    newPlayer.UpdatePosition(posX, posY);
                    remotePlayers[playerName] = newPlayer;
                }

                updatedPlayers[playerName] = true;
            }

            // 기존 플레이어 중에서 업데이트되지 않은 플레이어 삭제
            var playersToDelete = new List<string>();
            foreach (var kvp in remotePlayers)
            {
                if (!updatedPlayers.ContainsKey(kvp.Key))
                {
                    playersToDelete.Add(kvp.Key);
                }
            }

            foreach (var playerName in playersToDelete)
            {
                remotePlayers.Remove(playerName);
            }
        }
    }

    private void Cleanup()
    {
        remotePlayers.Clear();
    }


    ~GameWorld()
    {
        Cleanup();
    }
}
