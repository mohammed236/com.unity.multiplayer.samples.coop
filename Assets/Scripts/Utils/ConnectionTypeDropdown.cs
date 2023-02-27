using TMPro;
using UnityEngine;

namespace Unity.BossRoom.Utils
{
    public class ConnectionTypeDropdown : MonoBehaviour
    {
        enum ConnectionType
        {
            UDP,
            DTLS,
            WS,
            WSS,
        }

        [SerializeReference]
        TMP_Dropdown m_Dropdown;

        public static string connectionType { get; set; } = "udp";

        void Start()
        {
            Initialize();

            m_Dropdown.onValueChanged.AddListener(delegate {
                DropdownValueChanged(m_Dropdown.value);
            });
        }

        void Initialize()
        {
            switch (connectionType)
            {
                case "udp":
                    m_Dropdown.value = (int)ConnectionType.UDP;
                    break;
                case "dtls":
                    m_Dropdown.value = (int)ConnectionType.DTLS;
                    break;
                case "ws":
                    m_Dropdown.value = (int)ConnectionType.WS;
                    break;
                case "wss":
                    m_Dropdown.value = (int)ConnectionType.WSS;
                    break;
            }
        }

        void DropdownValueChanged(int value)
        {
            switch (value)
            {
                case (int)ConnectionType.UDP:
                    connectionType = "udp";
                    Debug.Log("UDP");
                    break;
                case (int)ConnectionType.DTLS:
                    connectionType = "dtls";
                    Debug.Log("DTLS");
                    break;
                case (int)ConnectionType.WS:
                    connectionType = "ws";
                    Debug.Log("WS");
                    break;
                case (int)ConnectionType.WSS:
                    connectionType = "wss";
                    Debug.Log("WSS");
                    break;
            }
        }
    }
}
