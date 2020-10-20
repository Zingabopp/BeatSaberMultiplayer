using BeatSaberMultiplayerLite.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Lidgren.Network;
using BS_Utils.Gameplay;
using System.Reflection;
using BeatSaberMultiplayerLite.UI.ViewControllers.ServerHubScreen;
using HMUI;
using BeatSaberMarkupLanguage;
using BS_Utils.Utilities;
using UnityEngine.Networking;
using System.Collections.Concurrent;
using System.IO;

namespace BeatSaberMultiplayerLite.UI.FlowCoordinators
{

    public struct ServerHubRoom
    {
        public string ip;
        public int port;
        public RoomInfo roomInfo;

        public ServerHubRoom(string ip, int port, RoomInfo roomInfo)
        {
            this.ip = ip;
            this.port = port;
            this.roomInfo = roomInfo;
        }
    }

    class ServerHubFlowCoordinator : FlowCoordinator
    {
        RoomListViewController _roomListViewController;

        List<ServerHubClient> _serverHubClients = new List<ServerHubClient>();

        List<ServerHubRoom> _roomsList = new List<ServerHubRoom>();
        bool _roomsListDirty = false;

        public bool doNotUpdate = false;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation && addedToHierarchy)
            {
                SetTitle("Online Multiplayer");

                _roomListViewController = BeatSaberUI.CreateViewController<RoomListViewController>();

                _roomListViewController.createRoomButtonPressed += CreateRoomPressed;
                _roomListViewController.selectedRoom += RoomSelected;
                _roomListViewController.refreshPressed += RefreshPressed;
            }

            showBackButton = true;

            ProvideInitialViewControllers(_roomListViewController, null, null);

            StartCoroutine(GetServersFromRepositories());
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            if (topViewController == _roomListViewController)
            {
                PluginUI.instance.modeSelectionFlowCoordinator.InvokeMethod("DismissFlowCoordinator", this, null, false);
            }
        }
        private bool refreshingRoomsList = false;
        private void RefreshPressed()
        {
            _roomsListDirty = true;
            StartCoroutine(UpdateRoomsListCoroutine());
        }

        private void CreateRoomPressed()
        {
            PresentFlowCoordinator(PluginUI.instance.roomCreationFlowCoordinator, null, ViewController.AnimationDirection.Horizontal, false, false);
            PluginUI.instance.roomCreationFlowCoordinator.SetServerHubsList(_serverHubClients);

            PluginUI.instance.roomCreationFlowCoordinator.didFinishEvent -= RoomCreationFlowCoordinator_didFinishEvent;
            PluginUI.instance.roomCreationFlowCoordinator.didFinishEvent += RoomCreationFlowCoordinator_didFinishEvent;
        }

        private void RoomCreationFlowCoordinator_didFinishEvent(bool immediately)
        {
            PluginUI.instance.roomCreationFlowCoordinator.didFinishEvent -= RoomCreationFlowCoordinator_didFinishEvent;
            try
            {
                DismissFlowCoordinator(PluginUI.instance.roomCreationFlowCoordinator, ViewController.AnimationDirection.Horizontal, null, immediately);
            }
            catch (Exception e)
            {
                Plugin.log.Warn("Unable to dismiss flow coordinator! Exception: " + e);
            }
        }

        private void RoomSelected(ServerHubRoom selectedRoom, string password)
        {
            JoinRoom(selectedRoom.ip, selectedRoom.port, selectedRoom.roomInfo.roomId, selectedRoom.roomInfo.usePassword, password);
        }

        public void JoinRoom(string ip, int port, uint roomId, bool usePassword, string pass = "")
        {
            pass = pass?.ToUpper();
            PresentFlowCoordinator(PluginUI.instance.roomFlowCoordinator, null, ViewController.AnimationDirection.Horizontal, false, false);
            PluginUI.instance.roomFlowCoordinator.JoinRoom(ip, port, roomId, usePassword, pass);
            Client.Instance.inRadioMode = false;
            PluginUI.instance.roomFlowCoordinator.didFinishEvent -= RoomFlowCoordinator_didFinishEvent;
            PluginUI.instance.roomFlowCoordinator.didFinishEvent += RoomFlowCoordinator_didFinishEvent;
        }

        private void RoomFlowCoordinator_didFinishEvent()
        {
            PluginUI.instance.roomCreationFlowCoordinator.didFinishEvent -= RoomCreationFlowCoordinator_didFinishEvent;
            DismissFlowCoordinator(PluginUI.instance.roomFlowCoordinator, ViewController.AnimationDirection.Horizontal, null, false);
        }

        readonly WaitForSeconds roomRefreshDelay = new WaitForSeconds(1);
        public IEnumerator UpdateRoomsListCoroutine()
        {
#if DEBUG
            Plugin.log.Debug("Entering UpdateRoomsListCoroutine");
#endif
            if (refreshingRoomsList)
            {
#if DEBUG
                Plugin.log.Debug("UpdateRoomsListCoroutine already running.");
#endif
                yield break;
            }
            refreshingRoomsList = true;
            yield return null;
            if (doNotUpdate)
            {
                doNotUpdate = false;
                yield break;
            }
            UpdateRoomsList();

            while (_roomsListDirty)
            {
                _roomsListDirty = false;
                UpdateRoomsListUI();
                yield return roomRefreshDelay;
            }
#if DEBUG
            Plugin.log.Debug("Exiting UpdateRoomsListCoroutine");
#endif
            refreshingRoomsList = false;
        }

        protected IEnumerator GetServersFromRepositories()
        {
            Plugin.log.Debug("Starting GetServersFromRepositories");
            if (Config.Instance?.MultiplayerSettings.ServerRepositories == null)
                yield break;
            List<RepositoryServer> repoServers = new List<RepositoryServer>();
            string repoCachePath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, "ServerRepositoryCache.json");
            ServerRepositoryCache repoCache = null;
            if (File.Exists(repoCachePath))
            {
                try
                {
                    repoCache = ServerRepositoryCache.FromJson(File.ReadAllText(repoCachePath));
                }
                catch (Exception ex)
                {
                    Plugin.log.Warn($"Unable to read ServerRepositoryCache.json: {ex.Message}");
                    Plugin.log.Debug(ex);
                }
            }
            else
            {
                Plugin.log.Debug($"ServerRepositoryCache.json not found, generating new one.");
            }
            if (repoCache == null)
                repoCache = new ServerRepositoryCache();
            int repositoriesUsed = 0;
            int serversAdded = 0;
            foreach (string serverRepoPath in Config.Instance.MultiplayerSettings.ServerRepositories)
            {
                Uri repoUri = null;
                ServerRepository repo = null;
                try
                {
                    repoUri = new Uri(serverRepoPath, UriKind.Absolute);
                }
                catch (Exception ex)
                {
                    Plugin.log.Warn($"Invalid server repository URL: {serverRepoPath}");
                    Plugin.log.Debug(ex);
                    continue;
                }
                UnityWebRequest www = UnityWebRequest.Get(repoUri);
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Plugin.log.Warn($"Error getting Server Repository: {serverRepoPath}");
                    Plugin.log.Debug(www.error);
                }
                else
                {
                    string serverRepoJsonStr = www.downloadHandler.text;
                    try
                    {
                        repo = ServerRepository.FromJson(serverRepoJsonStr);
                        if (repo != null)
                        {
                            if (repoCache.ServerRepositories.ContainsKey(serverRepoPath))
                                repoCache.ServerRepositories[serverRepoPath] = repo;
                            else
                                repoCache.ServerRepositories.Add(serverRepoPath, repo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.log.Warn($"Error parsing ServerRepository from {serverRepoPath}: {ex.Message}");
                        Plugin.log.Debug(ex);
                    }
                }
                if (repo == null && repoCache.ServerRepositories.TryGetValue(serverRepoPath, out repo))
                {
                    Plugin.log.Info($"Using cache of ServerRepository '{serverRepoPath}' with {repo.Servers.Count} servers.");
                }
                if (repo != null)
                {
                    bool repositoryUsed = false;
                    foreach (var server in repo.Servers)
                    {
                        Plugin.log.Debug($"Server: {server.ToString()}");
                        if (server.IsValid)
                        {
                            if (!repoServers.Any(s => s.ServerId.Equals(server.ServerId, StringComparison.OrdinalIgnoreCase)))
                            {
                                repoServers.Add(server);
                                serversAdded++;
                                repositoryUsed = true;
                            }
                            else
                            {
                                Plugin.log.Debug($"Skipping server '{server.ServerId}', already exists.");
                            }
                        }
                        else
                            Plugin.log.Warn($"Invalid server ({server.ToString()}) in repository {repo.RepositoryName}");
                    }
                    if (repositoryUsed)
                        repositoriesUsed++;
                }
            }

            if (serversAdded > 0)
            {
                RepositoryServers = repoServers.ToArray();
                Plugin.log.Debug($"Finished getting {(serversAdded == 1 ? $"{serversAdded} server" : $"{serversAdded} servers")} from {(repositoriesUsed == 1 ? $"{repositoriesUsed} server" : $"{repositoriesUsed} servers")}.");
            }
            else
                Plugin.log.Debug("Did not get any servers from server repositories.");
            try
            {
                File.WriteAllText(repoCachePath, repoCache.ToJson());
            }
            catch (Exception ex)
            {
                Plugin.log.Warn($"Failed to write ServerRepositoryCache to file: {ex.Message}");
                Plugin.log.Debug(ex);
            }
            yield return UpdateRoomsListCoroutine();
        }

        public RepositoryServer[] RepositoryServers { get; private set; }

        public void UpdateRoomsList()
        {
            Plugin.log.Info("Updating rooms list...");
            _serverHubClients.ForEach(x =>
            {
                if (x != null)
                {
                    x.Abort();
                    x.ReceivedRoomsList -= ReceivedRoomsList;
                    x.ServerHubException -= ServerHubException;
                }
            });
            _serverHubClients.Clear();
            _roomsList.Clear();

            // Store server addresses so duplicates aren't added.
            HashSet<string> serverAddresses = new HashSet<string>();
            if ((RepositoryServers?.Length ?? 0) > 0)
            {
                for (int i = 0; i < RepositoryServers.Length; i++)
                {
                    RepositoryServer repositoryServer = RepositoryServers[i];
                    string fullAddress = repositoryServer.ServerAddress + ":" + repositoryServer.ServerPort.ToString();
                    if (repositoryServer == null || !repositoryServer.IsValid)
                        continue;
                    if (serverAddresses.Contains(repositoryServer.ServerAddress))
                        continue;
                    serverAddresses.Add(fullAddress);
                    ServerHubClient client = repositoryServer.ToServerHubClient();
                    if (client != null)
                    {
                        client.ReceivedRoomsList += ReceivedRoomsList;
                        client.ServerHubException += ServerHubException;
                        _serverHubClients.Add(client);
                    }
                }
            }

            foreach (ServerHub hub in Config.Instance.MultiplayerSettings.ServerHubs)
            {
                string ip = hub.Address;
                if (string.IsNullOrEmpty(ip))
                {
                    Plugin.log.Warn($"ServerHub entry with empty address in Config.");
                    continue;
                }
                int port = hub.Port;
                string fullAddress = ip + ":" + port.ToString();
                if (serverAddresses.Contains(fullAddress))
                    continue;
                serverAddresses.Add(fullAddress);
                ServerHubClient client = new GameObject("ServerHubClient").AddComponent<ServerHubClient>();
                client.ip = ip;
                client.port = port;
                client.ReceivedRoomsList += ReceivedRoomsList;
                client.ServerHubException += ServerHubException;
                _serverHubClients.Add(client);
            }

            _roomListViewController.SetRooms(null);
            _roomListViewController.SetServerHubsCount(0, _serverHubClients.Count);
            _roomListViewController.SetRefreshButtonState(false);
            _serverHubClients.ForEach(x => x.GetRooms());

            Plugin.log.Info("Requested rooms lists from ServerHubs...");
        }

        private void ReceivedRoomsList(ServerHubClient sender, List<RoomInfo> rooms)
        {
            int roomsCount = rooms.Count;

            if (!string.IsNullOrEmpty(sender.serverHubName))
                Plugin.log.Debug($"Received {roomsCount} rooms from \"{sender.serverHubName}\" ({sender.ip}:{sender.port})! Total rooms count: {_roomsList.Count}. Time: {DateTime.UtcNow:ss.fff}");
            else
                Plugin.log.Debug($"Received {roomsCount} rooms from {sender.ip}:{sender.port}! Total rooms count: {_roomsList.Count}: Time: {DateTime.UtcNow:ss.fff}");

            if (roomsCount > 0) // don't bother doing more work if no rooms were received
            {
                _roomsList.AddRange(rooms.Select(x => new ServerHubRoom(sender.ip, sender.port, x)));
                _roomsListDirty = true;
                HMMainThreadDispatcher.instance.Enqueue(delegate ()
                {
                    StartCoroutine(UpdateRoomsListCoroutine());
                });
            }
        }

        /// <summary>
        /// Must be called from UI context.
        /// </summary>
        private void UpdateRoomsListUI()
        {
            _roomListViewController.SetRooms(_roomsList);
            _roomListViewController.SetServerHubsCount(_serverHubClients.Count(x => x.serverHubCompatible), _serverHubClients.Count);
            _roomListViewController.SetRefreshButtonState(true);

            if (PluginUI.instance.roomCreationFlowCoordinator.isActivated)
                PluginUI.instance.roomCreationFlowCoordinator.SetServerHubsList(_serverHubClients);
        }

        public void ServerHubException(ServerHubClient sender, Exception e)
        {
            string hubId = $"{sender.ip}:{sender.port}";
            string hubString;
            if (!string.IsNullOrEmpty(sender.serverHubName))
                hubString = $"\"{sender.serverHubName}\" ({hubId})";
            else
                hubString = $"({hubId})";
            bool logError = true;
            IPA.Logging.Logger.Level logLevel = IPA.Logging.Logger.Level.Error;
            ServerHubException hubException = e as ServerHubException;
            if (hubException != null)
            {
                if (HubLastExceptions.TryGetValue(hubId, out ServerHubExceptionType lastException))
                {
                    if (lastException == hubException.ServerHubExceptionType)
                        logError = false;
                    else
                        HubLastExceptions[hubId] = hubException.ServerHubExceptionType;
                }
                else
                    HubLastExceptions.TryAdd(hubId, hubException.ServerHubExceptionType);
                switch (hubException.ServerHubExceptionType)
                {
                    case ServerHubExceptionType.VersionMismatch:
                        logLevel = IPA.Logging.Logger.Level.Warning;
                        break;
                    case ServerHubExceptionType.PlayerNotWhitelisted:
                        logLevel = IPA.Logging.Logger.Level.Warning;
                        break;
                    case ServerHubExceptionType.PlayerBanned:
                        logLevel = IPA.Logging.Logger.Level.Warning;
                        break;
                    default:
                        break;
                }
            }
            if (logError)
            {
                Plugin.log.Log(logLevel, $"ServerHub exception : {hubString}: {e}");
            }
#if DEBUG
            else
            {

                Plugin.log.Debug($"Suppressed log: ServerHub exception ({hubId}): {e}");
            }
#endif
        }

        public ConcurrentDictionary<string, ServerHubExceptionType> HubLastExceptions = new ConcurrentDictionary<string, ServerHubExceptionType>();
    }

    public static class ServerHubExtensions
    {
        /// <summary>
        /// Converts a <see cref="RepositoryServer"/> to a <see cref="ServerHubClient"/>. Returns null if the <see cref="RepositoryServer"/> has invalid values.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static ServerHubClient ToServerHubClient(this RepositoryServer server)
        {
            if (string.IsNullOrEmpty(server?.ServerAddress) || server.ServerPort < 1 || server.ServerPort > 65535)
                return null;
            ServerHubClient client = new GameObject($"ServerHubClient.{server.ServerName ?? server.ServerAddress}").AddComponent<ServerHubClient>();
            client.ip = server.ServerAddress;
            client.port = server.ServerPort;
            client.serverHubName = server.ServerName;
            return client;
        }
    }

    public class ServerHubClient : MonoBehaviour
    {
        private NetClient NetworkClient;

        public string serverHubName = "";
        public string ip;
        public int port;

        public bool serverHubAvailable;
        public bool serverHubCompatible;
        public int availableRoomsCount;
        public int playersCount;

        public float ping;

        public List<RoomInfo> availableRooms = new List<RoomInfo>();

        public event Action<ServerHubClient, List<RoomInfo>> ReceivedRoomsList;
        public event Action<ServerHubClient, Exception> ServerHubException;

        public void Awake()
        {
            NetPeerConfiguration Config = new NetPeerConfiguration("BeatSaberMultiplayer");
            NetworkClient = new NetClient(Config);
        }

        public void GetRooms()
        {
            try
            {
                Task.Run(() =>
                {
                    NetworkClient.Start();

                    Plugin.log.Debug($"Creating message...");
                    NetOutgoingMessage outMsg = NetworkClient.CreateMessage();

                    Version assemblyVersion = Plugin.ClientCompatibilityVersion;
                    byte[] version = new byte[4] { (byte)assemblyVersion.Major, (byte)assemblyVersion.Minor, (byte)assemblyVersion.Build, (byte)assemblyVersion.Revision };

                    outMsg.Write(version);
                    new PlayerInfo(Plugin.Username, Plugin.UserId).AddToMessage(outMsg);

                    Plugin.log.Debug($"Connecting to {ip}:{port}...");

                    NetworkClient.Connect(ip, port, outMsg);
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ServerHubException?.Invoke(this, e);
                Abort();
            }
        }

        public void Update()
        {
            if (NetworkClient != null && NetworkClient.Status == NetPeerStatus.Running)
            {
                NetIncomingMessage msg;
                while ((msg = NetworkClient.ReadMessage()) != null)
                {
                    if (NetworkClient.Connections.FirstOrDefault() != null)
                    {
                        ping = Math.Abs(NetworkClient.Connections.First().AverageRoundtripTime);
                    }
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.StatusChanged:
                            {
                                NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();

                                if (status == NetConnectionStatus.Connected)
                                {
                                    if (NetworkClient.ServerConnection != null && NetworkClient.ServerConnection.RemoteHailMessage != null && NetworkClient.ServerConnection.RemoteHailMessage.LengthBytes > 4)
                                    {
                                        var hailMsg = NetworkClient.ServerConnection.RemoteHailMessage;

                                        try
                                        {
                                            byte[] serverVer = hailMsg.ReadBytes(4);
                                            string serverNameResponse = hailMsg.ReadString();
                                            if (!string.IsNullOrEmpty(serverNameResponse))
                                                serverHubName = serverNameResponse;
                                        }
                                        catch (Exception e)
                                        {
                                            Plugin.log.Warn("Unable to read additional ServerHub info! Exception: " + e);
                                        }
                                    }

                                    serverHubCompatible = true;
                                    serverHubAvailable = true;
                                    NetOutgoingMessage outMsg = NetworkClient.CreateMessage();
                                    outMsg.Write((byte)CommandType.GetRooms);

                                    NetworkClient.SendMessage(outMsg, NetDeliveryMethod.ReliableOrdered, 0);
                                }
                                else if (status == NetConnectionStatus.Disconnected)
                                {
                                    ServerHubExceptionType serverHubExceptionType = ServerHubExceptionType.None;
                                    bool formatError = true;
                                    try
                                    {
                                        string reason = msg.ReadString();
                                        formatError = false;
                                        if (reason.Contains("Version mismatch"))
                                        {
                                            serverHubCompatible = false;
                                            serverHubAvailable = true;
                                            serverHubExceptionType = ServerHubExceptionType.VersionMismatch;
                                        }
                                        else
                                        {
                                            serverHubCompatible = false;
                                            serverHubAvailable = false;
                                        }
                                        if (reason.Contains("not whitelisted"))
                                            serverHubExceptionType = ServerHubExceptionType.PlayerNotWhitelisted;
                                        else if (reason.Contains("banned"))
                                            serverHubExceptionType = ServerHubExceptionType.PlayerBanned;
                                        ServerHubException?.Invoke(this, new ServerHubException("ServerHub refused connection! Reason: " + reason, serverHubExceptionType));
                                    }
                                    catch (Exception e)
                                    {
                                        if (formatError)
                                            ServerHubException?.Invoke(this, new ServerHubException("ServerHub refused connection! Exception: " + e, ServerHubExceptionType.MalformedPacket));
                                        else
                                            ServerHubException?.Invoke(this, new ServerHubException("ServerHub refused connection! Exception: " + e, ServerHubExceptionType.None));
                                    }
                                    Abort();
                                }

                            };
                            break;

                        case NetIncomingMessageType.Data:
                            {
                                if ((CommandType)msg.ReadByte() == CommandType.GetRooms)
                                {
                                    try
                                    {
                                        int roomsCount = msg.ReadInt32();

                                        availableRooms.Clear();

                                        for (int i = 0; i < roomsCount; i++)
                                        {
                                            availableRooms.Add(new RoomInfo(msg));
                                        }

                                        availableRoomsCount = availableRooms.Count;
                                        playersCount = availableRooms.Sum(x => x.players);
                                        ReceivedRoomsList?.Invoke(this, availableRooms);
                                    }
                                    catch (Exception e)
                                    {
                                        ServerHubException?.Invoke(this, new ServerHubException("Unable to parse rooms list! Exception: " + e, ServerHubExceptionType.MalformedPacket));
                                    }
                                    Abort();
                                }
                            };
                            break;
                        case NetIncomingMessageType.WarningMessage:
                            Plugin.log.Warn(msg.ReadString());
                            break;
                        case NetIncomingMessageType.ErrorMessage:
                            Plugin.log.Error(msg.ReadString());
                            break;
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                            Plugin.log.Debug(msg.ReadString());
                            break;
                        default:
                            Plugin.log.Debug("Unhandled type: " + msg.MessageType);
                            break;
                    }

                }
            }
        }

        public void Abort()
        {
            NetworkClient.Shutdown("");
            availableRooms.Clear();
            Destroy(gameObject);
        }
    }
}
