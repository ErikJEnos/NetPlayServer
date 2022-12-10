using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;

    public LinkedList<GameRoom> gameRoomList;
    public LinkedList<PlayerAccount> playerAccountList;
    public int waitingPlayerId = -1;
    public int playerTurn = 1;

    public List<string> chatlog;

    public List<string> PlaysArray;

    public class GameRoom
    {
        public string roomName;
        public int player1Id = 1, player2Id, spectator = 0;
        public bool gameRunning = false;

        public GameRoom(int Player1Id, int Player2Id, int Spectator, string RoomName, bool GameRunning)
        {
            player1Id = Player1Id;
            player2Id = Player2Id;
            spectator = Spectator;
            roomName = RoomName;
            gameRunning = GameRunning;
        }

    }

    public class PlayerAccount
    {
        public string playerName, playerPassword, playerId;

        public PlayerAccount(string PlayerName, string PlayerPassword, string PlayerId)
        {
            playerName = PlayerName;
            playerPassword = PlayerPassword;
            playerId = PlayerId;
        }
    }

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


    // Start is called before the first frame update
    void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);


        gameRoomList = new LinkedList<GameRoom>();
        playerAccountList = new LinkedList<PlayerAccount>();

    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }
  
    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }
    
    private void ProcessRecievedMsg(string msg, int id)
    {

        string[] temp = msg.Split(',');
        int signifierID = int.Parse(temp[0]);

        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        if (signifierID == ClientToServerSignifiers.CreatePlayerAccount)
        {
            CreateLoginAccount(temp, id);
            SendMessageToClient(ServerToClientSignifiers.AccountComplete + ",", id);
        }
        
        if (signifierID == ClientToServerSignifiers.Login)
        {
            LoginAccount(temp, id);
        }

        if (signifierID == ClientToServerSignifiers.CreateGameRoom)
        {
            CreateRoomSevrer(temp, id);
        }

        if (signifierID == ClientToServerSignifiers.RemovePlayerGameRoom)
        {
            RemovePlayerCreateRoomSevrer(id);
        }

        if (signifierID == ClientToServerSignifiers.TicTacToePlay)
        {
            TicTacToePlay(temp[1], id);
        }

        if (signifierID == ClientToServerSignifiers.PlayerWins)
        {
            PlayerWins(temp[1], id);
        }

        if (signifierID == ClientToServerSignifiers.LockPlayerControls)
        {
            LockControls(id);
        }

        if (signifierID == ClientToServerSignifiers.ChatLogMessage)
        {
            ChatLog(temp, id);
        }


    }

    public void CreateLoginAccount(string[] usernamePassword, int id)
    {
        Debug.Log("Creating account... username: " + usernamePassword[1] + " Password: " + usernamePassword[2]);

        DirectoryInfo[] cDirs = new DirectoryInfo(@"c:\").GetDirectories();

        using (StreamWriter sw = new StreamWriter("PlayerAccounts.txt"))
        {
            sw.WriteLine(usernamePassword[1] + "," + usernamePassword[2]);
           
            PlayerAccount player = new PlayerAccount(usernamePassword[1], usernamePassword[2], id.ToString());
            playerAccountList.AddLast(player);
        }
        
        
    }

    public void LoginAccount(string[] usernamePassword, int id)
    {
        string line;
        string[] AccountArray;

        using (StreamReader sr = new StreamReader("PlayerAccounts.txt"))
        {

            while ((line = sr.ReadLine()) != null)
            {
                AccountArray = line.Split(',');

                if (AccountArray[0] == usernamePassword[1] && AccountArray[1] == usernamePassword[2])
                {
                    SendMessageToClient(ServerToClientSignifiers.LoginSuccessfull + ",", id);
                }
                else
                {
                    SendMessageToClient(ServerToClientSignifiers.LoginFailed + ",", id);
                }
            }
        }
    }

    public void CreateRoomSevrer(string[] player, int id)
    {
        if (waitingPlayerId == -1)
        {
            waitingPlayerId = id;
            Debug.Log("waitingPlayerId: " + waitingPlayerId);
            SendMessageToClient(ServerToClientSignifiers.WaitingForAnotherPlayer + ",", id);

            GameRoom gameRoom = new GameRoom(waitingPlayerId, -1, -1, player[1], false);
            gameRoomList.AddLast(gameRoom);
        }
        else
        {
            if(gameRoomList.Count <= 0)
            {
                GameRoom gameRoom = new GameRoom(waitingPlayerId, id, -1, player[1], false);
                gameRoomList.AddLast(gameRoom);
            }
            else
            {
                foreach (GameRoom x in gameRoomList)
                {
                    Debug.Log(x.roomName);
                    if (x.roomName != player[1])
                    {
                        GameRoom gameRoom = new GameRoom(waitingPlayerId, id, -1, player[1], false);
                        gameRoomList.AddLast(gameRoom);
                    }
                    else if (x.roomName == player[1] && x.gameRunning == false)
                    {
                        Debug.Log("Room has already been made. connceting you to room");
                        x.gameRunning = true;
                        x.player1Id = waitingPlayerId;
                        x.player2Id = id;

                        SendMessageToClient(ServerToClientSignifiers.EnterPlayState + ",", x.player1Id);
                        SendMessageToClient(ServerToClientSignifiers.EnterPlayState + ",", x.player2Id);

                        foreach (GameRoom c in gameRoomList)
                        {
                            Debug.Log(c.player1Id + ":" + c.player2Id);
                        }
                    }
                    else if (x.roomName == player[1] && x.gameRunning == true)
                    {
                        x.spectator = id;
                        SendMessageToClient(ServerToClientSignifiers.EnterPlayState + ",", x.spectator);
                        Debug.Log("Player1ID: " + x.player1Id + "Player2ID: " + x.player2Id + "spectator: " + x.spectator);
                    }
                }
            }
           
        }
    }

    public void RemovePlayerCreateRoomSevrer(int id)
    {
        foreach (GameRoom x in gameRoomList)
        { 
            if(x.player1Id == id)
            {
                Debug.Log("player1Id was removed: " + x.player1Id);
                x.player1Id = 0;
                
            }
            if(x.player2Id == id)
            {
                Debug.Log("player2Id was removed: " + x.player2Id);
                x.player2Id = 0;
            }
            Debug.Log(x.player1Id +" : "+ x.player2Id);

        }
    }

    public void TicTacToePlay(string position, int id)
    {
        if (id == 1 && playerTurn == 1)
        {
            Debug.Log("Player1 to working");
            RecordPlays(position, id, 'X');
            SendMessageToClient(ServerToClientSignifiers.playTile + "," + "X" + "," + position, 2);
            SendMessageToClient(ServerToClientSignifiers.playTile + "," + "X" + "," + position, 1);
            SendMessageToClient(ServerToClientSignifiers.playTile + "," + "X" + "," + position, 3);
            SendMessageToClient(ServerToClientSignifiers.playTile + "," + "X" + "," + position, 3);
            playerTurn = 2;
        }
        if (id == 2 && playerTurn == 2)
        {
            Debug.Log("Player2 to working");
            RecordPlays(position, id, 'O');
            SendMessageToClient(ServerToClientSignifiers.playTile + "," + "O" + "," + position, 1);
            SendMessageToClient(ServerToClientSignifiers.playTile + "," + "O" + "," + position, 2);
            SendMessageToClient(ServerToClientSignifiers.playTile + "," + "O" + "," + position, 3);
            SendMessageToClient(ServerToClientSignifiers.playTile + "," + "O" + "," + position, 3);
            playerTurn = 1;
        }
    }

    public void PlayerWins(string winningPlayer, int id)
    {
        Debug.Log("asdaasdsasdadsadsadsasadsd");
        if(winningPlayer == "X")
        {
            SendMessageToClient(ServerToClientSignifiers.Winner + ",", 1);
            SendMessageToClient(ServerToClientSignifiers.Loser + ",", 2);
        }
        else if (winningPlayer == "O")
        {
            SendMessageToClient(ServerToClientSignifiers.Winner + ",", 2);
            SendMessageToClient(ServerToClientSignifiers.Loser + ",", 1);
        }
        SendMessageToClient(ServerToClientSignifiers.Replay + ",", id);
        StartCoroutine(PlaysRecord(id));
    }

    public void LockControls(int id)
    {
        if(id == 1)
        {
            SendMessageToClient(ServerToClientSignifiers.LockPlayerControls + ",", 2);
            
        }
        else
        {
            SendMessageToClient(ServerToClientSignifiers.LockPlayerControls + ",", 1);
            
        }

        Debug.Log("Locking Player Controls: " + id);
    }

    public void ChatLog(string[] msg, int id)
    {
        chatlog.Add(msg[1]);

        for(int x = 0; x < chatlog.Count; x++)
        {
            Debug.Log("Sever Chat log: " + chatlog[x]);
            
        }

        SendMessageToClient(ServerToClientSignifiers.ChatLogMessage + "," + msg[1], 3);
        SendMessageToClient(ServerToClientSignifiers.ChatLogMessage + "," + msg[1], 2);
        SendMessageToClient(ServerToClientSignifiers.ChatLogMessage + "," + msg[1], 1);

    }

    public void RecordPlays(string pos, int id, char sym)
    {
        PlaysArray.Add(sym + "," + pos);
        
        string line = "";

        using (StreamWriter sw = new StreamWriter("Plays.txt"))
        {
            foreach(string x in PlaysArray)
            {
                sw.WriteLine(x);
            }
        }
    }

    IEnumerator PlaysRecord(int id)
    {
        
        DirectoryInfo[] cDirs = new DirectoryInfo(@"c:\").GetDirectories();

        StreamReader sr = new StreamReader("Plays.txt");

        for(int x = 0; x < PlaysArray.Count; x++)
        {
            yield return new WaitForSeconds(2);
            SendMessageToClient(ServerToClientSignifiers.playTile + "," + PlaysArray[x], id);

        }


        //string line = "";
        //while ((line = sr.ReadLine()) != null)
        //{
        //    SendMessageToClient(ServerToClientSignifiers.playTile + "," + sr.ReadLine(), 1);
        //    Debug.Log(line);
        //    yield return new WaitForSeconds(2);
        //}
    }

}
