using System;
using System.Threading.Tasks;
using Characters;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UI
{
    public class MainMenu:MonoBehaviour
    {
        [SerializeField] private Button _newGame;
        [SerializeField] private Button _joinGame;
        [SerializeField] private TMP_InputField _login;
        [SerializeField] private TMP_InputField _ip;
        [SerializeField] private GameObject _buttonsHolder;
        private NetworkManager _manager;
        private string _playerName;
        private bool _isStart = false;
        public string PlayerName => _playerName;

        private void Awake()
        {
            _manager = Object.FindObjectOfType<NetworkManager>();
            _newGame.onClick.AddListener(NewGame);
            _joinGame.onClick.AddListener(JoinGame);
        }

        public void SetAction(ShipController controller)
        {
            controller.Restart += JoinGame;
        }

        private void JoinGame()
        {
            _manager.StartClient();
            _manager.networkAddress = _ip.text;
            _playerName = _login.text;
            _isStart = true;
        }

        private void NewGame()
        {
            _manager.StartHost();
            _playerName = _login.text;
            _isStart = true;
        }

        private void Update()
        {
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                if (!_buttonsHolder.activeSelf)
                    _buttonsHolder.SetActive(true);
            }
            else
            {
                if (_buttonsHolder.activeSelf)
                    _buttonsHolder.SetActive(false);            
            }
            if (NetworkClient.isConnected && !NetworkClient.ready)
            {
                NetworkClient.Ready();
            }
        }
    }
}