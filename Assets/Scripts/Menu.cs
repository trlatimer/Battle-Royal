using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class Menu : MonoBehaviourPunCallbacks, ILobbyCallbacks
{
    [Header("Screens")]
    public GameObject mainScreen;
    public GameObject createRoomScreen;
    public GameObject lobbyScreen;
    public GameObject lobbyBrowserScreen;

    [Header("Main Screen")]
    public Button createRoomButton;
    public Button findRoomButton;

    [Header("Lobby Screen")]
    public TextMeshProUGUI playerListText;
    public TextMeshProUGUI roomInfoText;
    public Button startGameButton;

    [Header("Lobby Browser")]
    public RectTransform roomListContainer;
    public GameObject roomButtonPrefab;

    private List<GameObject> roomButtons = new List<GameObject>();
    private List<RoomInfo> roomList = new List<RoomInfo>();

    private void Start()
    {
        // Disable menu buttons
        createRoomButton.interactable = false;
        findRoomButton.interactable = false;

        // Enable cursor
        Cursor.lockState = CursorLockMode.None;

        // Check if in game
        if (PhotonNetwork.InRoom)
        {
            // Go to Lobby

            // Make room visible again
            PhotonNetwork.CurrentRoom.IsVisible = true;
            PhotonNetwork.CurrentRoom.IsOpen = true;
        }
    }

    // Changes visibility of screen
    private void SetScreen(GameObject screen)
    {
        // Disable other screens
        mainScreen.SetActive(false);
        createRoomScreen.SetActive(false);
        lobbyScreen.SetActive(false);
        lobbyBrowserScreen.SetActive(false);

        // Activate requested screen
        screen.SetActive(true);

        if (screen == lobbyBrowserScreen)
        {
            UpdateLobbyBrowserUI();
        }
    }

    // Called when Back button pressed
    public void OnBackButton()
    {
        SetScreen(mainScreen);
    }

    #region MAIN SCREEN
    public void OnPlayerNameValueChanged(TMP_InputField playerNameInput)
    {
        PhotonNetwork.NickName = playerNameInput.text;
    }

    public override void OnConnectedToMaster()
    {
        // Enable menu buttons once connected
        createRoomButton.interactable = true;
        findRoomButton.interactable = true;
    }

    public void OnCreateRoomButton()
    {
        SetScreen(createRoomScreen);
    }

    public void OnFindRoomButton()
    {
        SetScreen(lobbyBrowserScreen);
    }
    #endregion

    #region CREATE SCREEN
    public void OnCreateButton(TMP_InputField roomNameInput)
    {
        NetworkManager.instance.CreateRoom(roomNameInput.text);
    }
    #endregion

    #region LOBBY SCREEN
    public override void OnJoinedRoom()
    {
        SetScreen(lobbyScreen);
        photonView.RPC("UpdateLobbyUI", RpcTarget.All);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateLobbyUI();
    }

    [PunRPC]
    private void UpdateLobbyUI()
    {
        // Enable/disable Start Game button depending on if host
        startGameButton.interactable = PhotonNetwork.IsMasterClient;

        // Display all players
        playerListText.text = "";

        foreach (Player player in PhotonNetwork.PlayerList)
            playerListText.text += player.NickName + "\n";

        // Set room info text 
        roomInfoText.text = "<b>Room Name</b>\n" + PhotonNetwork.CurrentRoom.Name;
    }

    public void OnStartGameButton()
    {
        // Hide the room
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // Tell everyone to load the game
        NetworkManager.instance.photonView.RPC("ChangeScene", RpcTarget.All, "Game");
    }

    public void OnLeaveLobbyButton()
    {
        PhotonNetwork.LeaveRoom();
        SetScreen(mainScreen);
    }
    #endregion

    #region LOBBY BROWSER SCREEN

    GameObject CreateRoomButton()
    {
        GameObject buttonObj = Instantiate(roomButtonPrefab, roomListContainer.transform);
        roomButtons.Add(buttonObj);

        return buttonObj;
    }
    
    private void UpdateLobbyBrowserUI()
    {
        // Disable room buttons
        foreach (GameObject button in roomButtons)
            button.SetActive(false);

        // Display all current rooms in master server
        for(int x = 0; x < roomList.Count; x++)
        {
            // Get or create button
            GameObject button = x >= roomButtons.Count ? CreateRoomButton() : roomButtons[x];
            button.SetActive(true);

            // Set room name and player count text
            button.transform.Find("RoomNameText").GetComponent<TextMeshProUGUI>().text = roomList[x].Name;
            button.transform.Find("PlayerCountText").GetComponent<TextMeshProUGUI>().text = roomList[x].PlayerCount + " / " + roomList[x].MaxPlayers;

            // Set button OnClick event
            Button buttonComp = button.GetComponent<Button>();
            string roomName = roomList[x].Name;
            buttonComp.onClick.RemoveAllListeners();
            buttonComp.onClick.AddListener(() => { OnJoinRoomButton(roomName); });
        }
    }

    public void OnJoinRoomButton(string roomName)
    {
        NetworkManager.instance.JoinRoom(roomName);
    }

    public void OnRefreshButton()
    {
        UpdateLobbyBrowserUI();
    }

    public override void OnRoomListUpdate(List<RoomInfo> allRooms)
    {
        roomList = allRooms;
    }
    #endregion
}
