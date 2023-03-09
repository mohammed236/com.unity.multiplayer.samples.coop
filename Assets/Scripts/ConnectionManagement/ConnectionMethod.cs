using System;
using System.Threading.Tasks;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.BossRoom.Utils;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// ConnectionMethod contains all setup needed to setup NGO to be ready to start a connection, either host or client side.
    /// Please override this abstract class to add a new transport or way of connecting.
    /// </summary>
    public abstract class ConnectionMethodBase
    {
        protected ConnectionManager m_ConnectionManager;
        readonly ProfileManager m_ProfileManager;
        protected readonly string m_PlayerName;

        public abstract Task SetupHostConnectionAsync();

        public abstract Task SetupClientConnectionAsync();

        public ConnectionMethodBase(ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
        {
            m_ConnectionManager = connectionManager;
            m_ProfileManager = profileManager;
            m_PlayerName = playerName;
        }

        protected void SetConnectionPayload(string playerId, string playerName)
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                playerId = playerId,
                playerName = playerName,
                isDebug = Debug.isDebugBuild
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }

        protected string GetPlayerId()
        {
            if (Services.Core.UnityServices.State != ServicesInitializationState.Initialized)
            {
                return ClientPrefs.GetGuid() + m_ProfileManager.Profile;
            }

            return AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : ClientPrefs.GetGuid() + m_ProfileManager.Profile;
        }
    }

    /// <summary>
    /// Simple IP connection setup with UTP
    /// </summary>
    class ConnectionMethodIP : ConnectionMethodBase
    {
        string m_Ipaddress;
        ushort m_Port;

        private string CaCertificate =
            @"-----BEGIN CERTIFICATE-----
MIIDDzCCAfcCFBU2SZ+6r8bdx36uGkHroQpEmX+lMA0GCSqGSIb3DQEBCwUAMEQx
CzAJBgNVBAYTAkNBMQ8wDQYDVQQIDAZRdWViZWMxETAPBgNVBAcMCE1vbnRyZWFs
MREwDwYDVQQKDAhVbml0eSBDQTAeFw0yMjAzMTcwMzQyNDFaFw0zMjAzMTQwMzQy
NDFaMEQxCzAJBgNVBAYTAkNBMQ8wDQYDVQQIDAZRdWViZWMxETAPBgNVBAcMCE1v
bnRyZWFsMREwDwYDVQQKDAhVbml0eSBDQTCCASIwDQYJKoZIhvcNAQEBBQADggEP
ADCCAQoCggEBALv/mWjDrxtKTqKRrNBqZ9an0m60tSSNaXX9BRSOyGuqFmdEdW5v
YnQDXsn9wGKFF6mgr2ATfgL273Im95aLRvHwhNmEP2c2T6WUq//Pq32nJ8kwiKly
2ctBdp6QyxgRuKMvFhTFAjzEdwH6GNWdmDjq3BgErKH8JhBnAzV5DAdbnr0pC4es
0ZAOVw8iyxKWW9U6/pb/Jed6R/ioV6OuGQbaAfGhFO2/lt3RYI4MkUr9pTWIqJwc
aDL9WxCoTggVNkckQmlMiLe0rYcCqEc+A0MdWblVNKds6HcBxyjMgxsELA4DmQ7C
4frNN8EtokxbaqjbM/cJNYfQ9IoBsATKaHkCAwEAATANBgkqhkiG9w0BAQsFAAOC
AQEAB7FwMBsB+pU6VBBGJPrHm70RitGffyDTefDtSOyrNXxdHyoMiSFhb26w/iin
/jubAZ5I3lvNFawRrDlzlJSxJDjaiHDd29W5UcV+6ij3Te/NJhck+9tXfuy6r95+
jjgGpm1RvBQq5XhEJh5FMfzXUYZ6NFg+6fLfqbE/hHo2mq+S0AAwR6gwDpr/6UzU
bARuY+bmrEFjEVFXNkmv4iZDkMQTi8UbmiwsNX3zJBPmSCErKiIPLHXBpzJitmcG
VYgO3hp/EObkBLHheqUuqLIY6XDvDhVPiJq4VyNGHnhR6GSiXs4ixL6v+UWrCHbh
ud3r5a40pzFbEWb6Zzrb3+BQZQ==
-----END CERTIFICATE-----";

        private string Certificate1 =
@"-----BEGIN CERTIFICATE-----
MIIDJzCCAg8CFDp7tjqt5Wu9lLe3VHOnx8nIXcI8MA0GCSqGSIb3DQEBCwUAMEQx
CzAJBgNVBAYTAkNBMQ8wDQYDVQQIDAZRdWViZWMxETAPBgNVBAcMCE1vbnRyZWFs
MREwDwYDVQQKDAhVbml0eSBDQTAeFw0yMjAzMTcwMzQ0MjJaFw0yMzAzMTcwMzQ0
MjJaMFwxCzAJBgNVBAYTAkNBMQ8wDQYDVQQIDAZRdWViZWMxETAPBgNVBAcMCE1v
bnRyZWFsMRUwEwYDVQQKDAxVbml0eSBTZXJ2ZXIxEjAQBgNVBAMMCTEyNy4wLjAu
MTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAOTUPP4Be094ZVYBR8Sr
Rt9AJVNF9EbHmn02BasRh6atEioWTRI77zO/Ul1YNd7b4zZczSAlvK5++q5Miqsi
rzZH4s5BOPl6iueWKKmhC0c8Mnp8uZtbDpAHAHImATb/o+UfZHPg243QQ64x6mSk
5p7OH4U8uM24cHOnMFxHA/PpEysviWcQseIbK8v4h4X+aL850EyeaXdlz6fEDnvJ
gwdYDf9KvQCluLLLswCLZ9R4Gy4NeYf8I5Ak8TuTwCpEzx/2EUOtq4cNgS//WGHn
s9nVgkTYRkWrrIUuFUy0KQxFTC/voZq9FgtG/zN4QPIwAN1HsTMndZ17APV4JdGr
tgMCAwEAATANBgkqhkiG9w0BAQsFAAOCAQEAYXfzhqmhKpHM5tsf53DQYgDbQgfS
bivFLCoo4iyUlNcEjoskv9oykDIr2CVGz7uYIgGAddoiTWv8T4GyTN6/xmE6YrO9
MS/wSLkAFJVqMeHfSWgu8ArZFPOZwc5qZCd211af8E9cyYxop/tGb6Dpa3Qpyj/o
A9F60aN7YOcvAILlq9zI1hWw3CCboW0BVXX/oHU2DHf17Lr5uMqmfjkKnITK6dqU
JyQyfBqQRdtLmjUi5NijpJqOISkv4rvDd0ahf9kOetH+AucKnQmbEEgUHIHKy8xx
IqjWfKWDGIrxnIiCnGBZ8DF/1mVndGsb+ufVdUB1A59CFJxgTXbBaSI7Vw==
-----END CERTIFICATE-----";

        // this will be required for DTLS and WSS, removed for security purpose, saved locally
        private string PrivateKey1 = "";

        public ConnectionMethodIP(string ip, ushort port, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            m_Ipaddress = ip;
            m_Port = port;
            m_ConnectionManager = connectionManager;
        }

        public override async Task SetupClientConnectionAsync()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName);
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            SetConnectionType(utp, false);
            utp.SetConnectionData(m_Ipaddress, m_Port);
            Debug.Log("[Use Encryption]: " + utp.UseEncryption);
            Debug.Log("[Use WebSockets]: " + utp.UseWebSockets);
        }

        public override async Task SetupHostConnectionAsync()
        {
            SetConnectionPayload(GetPlayerId(), m_PlayerName); // Need to set connection payload for host as well, as host is a client too
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            SetConnectionType(utp, true);
            utp.SetConnectionData(m_Ipaddress, m_Port);
            Debug.Log("[Use Encryption]: " + utp.UseEncryption);
            Debug.Log("[Use WebSockets]: " + utp.UseWebSockets);
        }

        void SetConnectionType(UnityTransport utp, bool isServer)
        {
            switch (ConnectionTypeDropdown.connectionType)
            {
                case "udp":
                    utp.UseEncryption = false;
                    utp.UseWebSockets = false;
                    break;

                case "dtls":
                    utp.UseEncryption = true;
                    utp.UseWebSockets = false;

                    if (isServer)
                    {
                        utp.SetServerSecrets(Certificate1, PrivateKey1);
                    }
                    else
                    {
                        utp.SetClientSecrets("127.0.0.1", CaCertificate);
                    }

                    break;

                case "ws":
                    utp.UseEncryption = false;
                    utp.UseWebSockets = true;
                    break;

                case "wss":
                    utp.UseEncryption = true;
                    utp.UseWebSockets = true;

                    if (isServer)
                    {
                        utp.SetServerSecrets(Certificate1, PrivateKey1);
                    }
                    else
                    {
                        utp.SetClientSecrets("127.0.0.1", CaCertificate);
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// UTP's Relay connection setup
    /// </summary>
    class ConnectionMethodRelay : ConnectionMethodBase
    {
        LobbyServiceFacade m_LobbyServiceFacade;
        LocalLobby m_LocalLobby;

        public ConnectionMethodRelay(LobbyServiceFacade lobbyServiceFacade, LocalLobby localLobby, ConnectionManager connectionManager, ProfileManager profileManager, string playerName)
            : base(connectionManager, profileManager, playerName)
        {
            m_LobbyServiceFacade = lobbyServiceFacade;
            m_LocalLobby = localLobby;
            m_ConnectionManager = connectionManager;
        }

        public override async Task SetupClientConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay client");

            SetConnectionPayload(GetPlayerId(), m_PlayerName);

            if (m_LobbyServiceFacade.CurrentUnityLobby == null)
            {
                throw new Exception("Trying to start relay while Lobby isn't set");
            }

            Debug.Log($"Setting Unity Relay client with join code {m_LocalLobby.RelayJoinCode}");

            // Create client joining allocation from join code
            var joinedAllocation = await RelayService.Instance.JoinAllocationAsync(m_LocalLobby.RelayJoinCode);
            Debug.Log($"client: {joinedAllocation.ConnectionData[0]} {joinedAllocation.ConnectionData[1]}, " +
                $"host: {joinedAllocation.HostConnectionData[0]} {joinedAllocation.HostConnectionData[1]}, " +
                $"client: {joinedAllocation.AllocationId}");

            await m_LobbyServiceFacade.UpdatePlayerRelayInfoAsync(joinedAllocation.AllocationId.ToString(), m_LocalLobby.RelayJoinCode);

            // Configure UTP with allocation
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            Debug.Log("Connection Type: " + ConnectionTypeDropdown.connectionType);
            utp.SetRelayServerData(new RelayServerData(joinedAllocation, ConnectionTypeDropdown.connectionType));
        }

        public override async Task SetupHostConnectionAsync()
        {
            Debug.Log("Setting up Unity Relay host");

            SetConnectionPayload(GetPlayerId(), m_PlayerName); // Need to set connection payload for host as well, as host is a client too

            // Create relay allocation
            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(m_ConnectionManager.MaxConnectedPlayers, region: null);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            Debug.Log($"server: connection data: {hostAllocation.ConnectionData[0]} {hostAllocation.ConnectionData[1]}, " +
                $"allocation ID:{hostAllocation.AllocationId}, region:{hostAllocation.Region}");

            m_LocalLobby.RelayJoinCode = joinCode;

            //next line enable lobby and relay services integration
            await m_LobbyServiceFacade.UpdateLobbyDataAsync(m_LocalLobby.GetDataForUnityServices());
            await m_LobbyServiceFacade.UpdatePlayerRelayInfoAsync(hostAllocation.AllocationIdBytes.ToString(), joinCode);

            // Setup UTP with relay connection info
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            Debug.Log("Connection Type: " + ConnectionTypeDropdown.connectionType);
            utp.SetRelayServerData(new RelayServerData(hostAllocation, ConnectionTypeDropdown.connectionType)); // This is with DTLS enabled for a secure connection
        }
    }
}
