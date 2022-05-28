using UnityEngine;

namespace UI
{
    public class Label : MonoBehaviour
    {
        private string _name => gameObject.name;
        private Camera _camera;
        

        private void OnGUI()
        {
            _camera = _camera == null ? Camera.main : _camera;
            if (_camera == null)
                return;

            var style = new GUIStyle();
            style.normal.background = Texture2D.redTexture;
            style.normal.textColor = Color.red;
            style.fontSize = 20;

            var position = _camera.WorldToScreenPoint(transform.position);

            var collider = GetComponent<Collider>();
            if (collider != null && _camera.Visible(collider))
            {
                GUI.Label(new Rect(new Vector2(position.x, Screen.height - position.y), new Vector2(20, _name.Length * 10.5f)), _name, style);
            }
        }
    }
}
