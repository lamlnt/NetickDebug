using Netick.Unity;
using UnityEngine;

public class RandomMovement : NetworkBehaviour
{
    [SerializeField] float _speed;
    public override void NetworkFixedUpdate()
    {
        base.NetworkFixedUpdate();
        
        if (gameObject != null && gameObject.transform != null)
            gameObject.transform.position += _speed * Sandbox.DeltaTime * Vector3.right;
        
        
    }
}