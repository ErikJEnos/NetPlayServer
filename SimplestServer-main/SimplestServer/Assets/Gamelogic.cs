using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class Gamelogic : MonoBehaviour
{

    LinkedList<int> connectedClientIDs;

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



    // Start is called before the first frame update
    void Start()
    {
        NetworkedServerProcessing.SetGameLogic(this);

        connectedClientIDs = new LinkedList<int>();

        gameRoomList = new LinkedList<GameRoom>();

        playerAccountList = new LinkedList<PlayerAccount>();
    }

    // Update is called once per frame
    void Update()
    {
        
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
                    NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.LoginSuccessfull + ",", id);
                }
                else
                {
                    NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.LoginFailed + ",", id);
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
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.WaitingForAnotherPlayer + ",", id);

            GameRoom gameRoom = new GameRoom(waitingPlayerId, -1, -1, player[1], false);
            gameRoomList.AddLast(gameRoom);
        }
        else
        {
            if (gameRoomList.Count <= 0)
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

                        NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.EnterPlayState + ",", x.player1Id);
                        NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.EnterPlayState + ",", x.player2Id);

                        foreach (GameRoom c in gameRoomList)
                        {
                            Debug.Log(c.player1Id + ":" + c.player2Id);
                        }
                    }
                    else if (x.roomName == player[1] && x.gameRunning == true)
                    {
                        x.spectator = id;
                        NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.EnterPlayState + ",", x.spectator);
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
            if (x.player1Id == id)
            {
                Debug.Log("player1Id was removed: " + x.player1Id);
                x.player1Id = 0;

            }
            if (x.player2Id == id)
            {
                Debug.Log("player2Id was removed: " + x.player2Id);
                x.player2Id = 0;
            }
            Debug.Log(x.player1Id + " : " + x.player2Id);

        }
    }

    public void TicTacToePlay(string position, int id)
    {
        if (id == 1 && playerTurn == 1)
        {
            Debug.Log("Player1 to working");
            RecordPlays(position, id, 'X');
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.playTile + "," + "X" + "," + position, 2);
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.playTile + "," + "X" + "," + position, 1);
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.playTile + "," + "X" + "," + position, 3);
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.playTile + "," + "X" + "," + position, 3);
            playerTurn = 2;
        }
        if (id == 2 && playerTurn == 2)
        {
            Debug.Log("Player2 to working");
            RecordPlays(position, id, 'O');
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.playTile + "," + "O" + "," + position, 1);
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.playTile + "," + "O" + "," + position, 2);
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.playTile + "," + "O" + "," + position, 3);
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.playTile + "," + "O" + "," + position, 3);
            playerTurn = 1;
        }
    }

    public void PlayerWins(string winningPlayer, int id)
    {
        Debug.Log("asdaasdsasdadsadsadsasadsd");
        if (winningPlayer == "X")
        {
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.Winner + ",", 1);
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.Loser + ",", 2);
        }
        else if (winningPlayer == "O")
        {
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.Winner + ",", 2);
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.Loser + ",", 1);
        }
        NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.Replay + ",", id);
        StartCoroutine(PlaysRecord(id));
    }

    public void LockControls(int id)
    {
        if (id == 1)
        {
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.LockPlayerControls + ",", 2);

        }
        else
        {
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.LockPlayerControls + ",", 1);

        }

        Debug.Log("Locking Player Controls: " + id);
    }

    public void ChatLog(string[] msg, int id)
    {
        chatlog.Add(msg[1]);

        for (int x = 0; x < chatlog.Count; x++)
        {
            Debug.Log("Sever Chat log: " + chatlog[x]);

        }

        NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.ChatLogMessage + "," + msg[1], 3);
        NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.ChatLogMessage + "," + msg[1], 2);
        NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.ChatLogMessage + "," + msg[1], 1);

    }

    public void RecordPlays(string pos, int id, char sym)
    {
        PlaysArray.Add(sym + "," + pos);

        string line = "";

        using (StreamWriter sw = new StreamWriter("Plays.txt"))
        {
            foreach (string x in PlaysArray)
            {
                sw.WriteLine(x);
            }
        }
    }

    public void AddConnectedClinet(int clientID)
    {
        foreach (GameRoom br in gameRoomList)
        {
            //string msg = br.Deserialize();
            //NetworkedServerProcessing.SendMessageToClient(msg, clientID);
        }
        connectedClientIDs.AddLast(clientID);
    }

    public void RemoveConnectedClinet(int clientID)
    {
        connectedClientIDs.Remove(clientID);
    }

    public string Deserialize()
    {
        //return ServerToClientSignifiers.BalloonSpawned + "," + xPosPercent + "," + yPosPercent + "," + id;
        return null;
    }


    IEnumerator PlaysRecord(int id)
    {

        DirectoryInfo[] cDirs = new DirectoryInfo(@"c:\").GetDirectories();

        StreamReader sr = new StreamReader("Plays.txt");

        for (int x = 0; x < PlaysArray.Count; x++)
        {
            yield return new WaitForSeconds(2);
            NetworkedServerProcessing.SendMessageToClient(ServerToClientSignifiers.playTile + "," + PlaysArray[x], id);

        }

    }

}
