using UnityEngine;
public class LockLocalPosition : MonoBehaviour
{
    private Transform _parent;
    private Vector3 _offset;
    private void Start()
    {
        _parent = transform.parent;
        _offset = transform.localPosition;
    }
    private void LateUpdate()
    {
        if (_parent != null)
            transform.localPosition = _offset;
    }
}