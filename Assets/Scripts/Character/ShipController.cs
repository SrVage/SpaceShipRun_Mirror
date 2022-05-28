using System;
using Main;
using Mechanics;
using Network;
using UI;
using UnityEngine;
using Mirror;

namespace Characters
{
    public class ShipController : NetworkMovableObject
    {
        public event Action Restart;
        [SerializeField] private Transform _cameraAttach;
        private CameraOrbit _cameraOrbit;
        private PlayerLabel playerLabel;
        private float _shipSpeed;
        private Rigidbody _rigidbody;
        private Vector3 _spawnPoint;

        [SyncVar] private string _playerName;

        private Vector3 currentPositionSmoothVelocity;

        protected override float speed => _shipSpeed;

        private void OnGUI()
        {
            if (_cameraOrbit == null)            
                return;
            
            _cameraOrbit.ShowPlayerLabels(playerLabel);
        }

        public override void OnStartAuthority()
        {
            _spawnPoint = transform.position;
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)            
                return;

            gameObject.name = FindObjectOfType<MainMenu>().PlayerName;
            FindObjectOfType<MainMenu>().SetAction(this);
            _cameraOrbit = FindObjectOfType<CameraOrbit>();
            _cameraOrbit.Initiate(_cameraAttach == null ? transform : _cameraAttach);
            playerLabel = GetComponentInChildren<PlayerLabel>();
            CmdRefreshName(gameObject.name);
            base.OnStartAuthority();
        }

        protected override void HasAuthorityMovement()
        {
            var spaceShipSettings = SettingsContainer.Instance?.SpaceShipSettings;
            if (spaceShipSettings == null)            
                return;            

            var isFaster = Input.GetKey(KeyCode.LeftShift);
            var speed = spaceShipSettings.ShipSpeed;
            var faster = isFaster ? spaceShipSettings.Faster : 1.0f;

            _shipSpeed = Mathf.Lerp(_shipSpeed, speed * faster, spaceShipSettings.Acceleration);

            var currentFov = isFaster ? spaceShipSettings.FasterFov : spaceShipSettings.NormalFov;
            _cameraOrbit.SetFov(currentFov, spaceShipSettings.ChangeFovSpeed);

            var velocity = _cameraOrbit.transform.TransformDirection(Vector3.forward) * _shipSpeed;
            _rigidbody.velocity = velocity * (_updatePhase == UpdatePhase.FixedUpdate ? Time.fixedDeltaTime : Time.deltaTime);

            if (!Input.GetKey(KeyCode.C))
            {
                var targetRotation = Quaternion.LookRotation(Quaternion.AngleAxis(_cameraOrbit.LookAngle, -transform.right) * velocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speed);
            }

            if (isServer)
            {
                SendToClients();
            }
            else
            {
                CmdSendTransform(transform.position, transform.rotation.eulerAngles);
            }
        }

        [Command]
        private void CmdRefreshName(string playerName)
        {
            _playerName = playerName;
            gameObject.name = playerName;
        }

        protected override void FromOwnerUpdate()
        {
            transform.position = Vector3.SmoothDamp(transform.position, serverPosition, ref currentPositionSmoothVelocity, speed);
            transform.rotation = Quaternion.Euler(serverEulers);                       
        }

        protected override void SendToClients()
        {
            serverPosition = transform.position;
            serverEulers = transform.eulerAngles;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasAuthority&&other.GetComponent<PlanetOrbit>())
            {
                transform.position = _spawnPoint;
                CmdSendTransform(transform.position, transform.rotation.eulerAngles);
            }
        }

        [Command]
        private void CmdSendTransform(Vector3 position, Vector3 eulers)
        {
            serverPosition = position;
            serverEulers = eulers;
        }

        [ClientCallback]
        private void LateUpdate()
        {
            _cameraOrbit?.CameraMovement();
        }
    }
}
