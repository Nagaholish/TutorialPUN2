using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// ロビー画面
/// 
/// controlPanelとprogressLabelの表示制御はお互い排他になっている
/// controlPanelがONのとき、通信前
/// progressLabelがONのとき、通信中
/// ※通信後は、違うシーンに飛ばされる
/// 
/// PhotonNetwork.IsConnected は PhotonNetwork.ConnectUsingSettings() を呼び出すまでfalseになっている
/// 
/// 
/// かんたんな処理の流れ
/// 「Play」の押下によって、Connectを呼び出す
///     JoinRandomRoomしようとして、誰もいない、もしくは人数オーバー
///         OnJoinRandomFailedがコールされる
///             ルームを生成を試みる（OnJoinRandomFailed）
///                 ルームを生成する
/// </summary>
public class Launcher : MonoBehaviourPunCallbacks
{
    #region Private Serialize Fields
    [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
    [SerializeField]
    private byte maxPlayersPerRoom = 4; //  １ROOMの最大参加人数

    [Tooltip("The Ui Panel to let the user enter name, connect and play")]
    [SerializeField]
    private GameObject controlPanel;

    [Tooltip("The UI Label to inform the user that the connection is in progress")]
    [SerializeField]
    private GameObject progressLabel;


    /// <summary>
    /// Keep track of the current process. Since connection is asynchronous and is based on several callbacks from Photon,
    /// we need to keep track of this to properly adjust the behavior when we receive call back by Photon.
    /// Typically this is used for the OnConnectedToMaster() callback.
    /// </summary>
    bool isConnecting;

    #endregion

    #region Private Fields

    string gameVersion = "1";

    #endregion

    /// <summary>
    /// Awake
    /// Photonのシーン同期を有効化する
    /// </summary>
    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        }
    
    /// <summary>
    /// Start
    /// ロビー用UIを初期化する
    /// </summary>
    void Start()
    {
        //Connect();

        progressLabel.SetActive(false);
        controlPanel.SetActive(true);

    }

    /// <summary>
    /// サーバへ接続を試みる
    /// </summary>
    public void Connect()
    {
        //  UIを通信中へ変更する
        progressLabel.SetActive(true);
        controlPanel.SetActive(false);

        //  サーバへ接続してみる
        //  始めはelse節へいく
        // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
        if (PhotonNetwork.IsConnected)  //  過去にサーバへ接続したことがあればIsConncectedはtrueになっているので、適当なルームへJoinしようとする
        {
            // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            // keep track of the will to join a room, because when we come back from the game we will get a callback that we are connected, so we need to know what to do then
            isConnecting = PhotonNetwork.ConnectUsingSettings();

            // #Critical, we must first and foremost connect to Photon Online Server.
            PhotonNetwork.GameVersion = gameVersion;
            //PhotonNetwork.ConnectUsingSettings();            
        }
    }

    #region MonoBehaviourPunCallbacks Callbacks

    /// <summary>
    /// マスターと接続完了コールバック
    /// 
    /// 接続の履歴があるかをチェックして、適当なルームへJoinする
    /// </summary>
    public override void OnConnectedToMaster()
    {
        // we don't want to do anything if we are not attempting to join a room.
        // this case where isConnecting is false is typically when you lost or quit the game, when this level is loaded, OnConnectedToMaster will be called, in that case
        // we don't want to do anything.
        if (isConnecting)
        {
            // #Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRandomFailed()
            PhotonNetwork.JoinRandomRoom();
            isConnecting = false;
        }
    }

    /// <summary>
    /// 通信切断コールバック
    /// </summary>
    /// <param name="cause"></param>
    public override void OnDisconnected(DisconnectCause cause)
    {
        progressLabel.SetActive(false);
        controlPanel.SetActive(true);

        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);

        isConnecting = false;
    }
    #endregion

    /// <summary>
    /// ジョイン失敗時コールバック
    /// 
    /// 人数オーバーか、部屋がないときは新しく部屋を生成する
    /// </summary>
    /// <param name="returnCode"></param>
    /// <param name="message"></param>
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
        //PhotonNetwork.CreateRoom(null, new RoomOptions());
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    /// <summary>
    /// 部屋に入れたとき（開設時含む）コールバック
    /// 
    /// 一人目の場合は、待ちぼうけ用の部屋へ飛ばす（Room for 1.unity）
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");

        // #Critical: We only load if we are the first player, else we rely on `PhotonNetwork.AutomaticallySyncScene` to sync our instance scene.
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            Debug.Log("We load the 'Room for 1' ");


            // #Critical
            // Load the Room Level.
            PhotonNetwork.LoadLevel("Room for 1");
        }
    }
}
