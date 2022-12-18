using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class NetworkedServerProcessing
{

    #region Send and Receive Data Functions
    static public void ReceivedMessageFromClient(string msg, int id)
    {
        Debug.Log("msg received = " + msg + ".  connection id = " + id);

        string[] csv = msg.Split(',');
        int signifier = int.Parse(csv[0]);

        string[] temp = msg.Split(',');
        int signifierID = int.Parse(temp[0]);

        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        if (signifierID == ClientToServerSignifiers.CreatePlayerAccount)
        {
            gameLogic.CreateLoginAccount(temp, id);
            SendMessageToClient(ServerToClientSignifiers.AccountComplete + ",", id);
        }

        if (signifierID == ClientToServerSignifiers.Login)
        {
            gameLogic.LoginAccount(temp, id);
        }

        if (signifierID == ClientToServerSignifiers.CreateGameRoom)
        {
            gameLogic.CreateRoomSevrer(temp, id);
        }

        if (signifierID == ClientToServerSignifiers.RemovePlayerGameRoom)
        {
            gameLogic.RemovePlayerCreateRoomSevrer(id);
        }

        if (signifierID == ClientToServerSignifiers.TicTacToePlay)
        {
            gameLogic.TicTacToePlay(temp[1], id);
        }

        if (signifierID == ClientToServerSignifiers.PlayerWins)
        {
            gameLogic.PlayerWins(temp[1], id);
        }

        if (signifierID == ClientToServerSignifiers.LockPlayerControls)
        {
            gameLogic.LockControls(id);
        }

        if (signifierID == ClientToServerSignifiers.ChatLogMessage)
        {
            gameLogic.ChatLog(temp, id);
        }


    }
    static public void SendMessageToClient(string msg, int clientConnectionID)
    {
        networkedServer.SendMessageToClient(msg, clientConnectionID);
    }

    #endregion

    #region Connection Events

    static public void ConnectionEvent(int clientConnectionID)
    {
        Debug.Log("New Connection, ID == " + clientConnectionID);
        gameLogic.AddConnectedClinet(clientConnectionID);
    }
    static public void DisconnectionEvent(int clientConnectionID)
    {
        Debug.Log("New Disconnection, ID == " + clientConnectionID);
        gameLogic.RemoveConnectedClinet(clientConnectionID);
    }

    #endregion

    #region Setup
    static NetworkedServer networkedServer;
    static Gamelogic gameLogic;

    static public void SetNetworkedServer(NetworkedServer NetworkedServer)
    {
        networkedServer = NetworkedServer;
    }
    static public NetworkedServer GetNetworkedServer()
    {
        return networkedServer;
    }
    static public void SetGameLogic(Gamelogic GameLogic)
    {
        gameLogic = GameLogic;
    }

    #endregion
}

#region Protocol Signifiers
public static class ClientToServerSignifiers
{
    public const int CreatePlayerAccount = 1;
    public const int Login = 2;
    public const int CreateGameRoom = 3;
    public const int RemovePlayerGameRoom = 4;
    public const int TicTacToePlay = 5;
    public const int PlayerWins = 6;
    public const int LockPlayerControls = 12;
    public const int ChatLogMessage = 13;
}

public static class ServerToClientSignifiers
{
    public const int AccountComplete = 01;
    public const int AccountFailed = 02;
    public const int LoginSuccessfull = 03;
    public const int LoginFailed = 04;
    public const int GameRoomSuccessfull = 05;
    public const int GameRoomFailed = 06;
    public const int WaitingForAnotherPlayer = 07;
    public const int EnterPlayState = 08;
    public const int playTile = 09;
    public const int Winner = 10;
    public const int Loser = 11;
    public const int LockPlayerControls = 12;
    public const int ChatLogMessage = 13;
    public const int Replay = 14;


}

#endregion

