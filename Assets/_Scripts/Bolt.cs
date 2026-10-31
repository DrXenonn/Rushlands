using System;
using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Bolt : NetworkBehaviour
{
    [SerializeField] private float Speed = 1;
    private short _damage;
    private UnitGroup _target;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(4);
        if (!IsServer) yield return null;
        NetworkObject.Despawn();
    }
    
    public void Init(UnitGroup target, short damage)
    {
        _target = target;
        _damage = damage;
        if (_target != null)
        {
            transform.LookAt(_target.transform.position);
            transform.Rotate(-90, 0, 0);
            StartCoroutine(nameof(Shoot));
        }
    }

    private IEnumerator Shoot()
    {
        while (_target != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, Speed * Time.deltaTime);

            yield return null;
        }

        NetworkObject.Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("UnitGroup"))
        {
            if (IsServer)
            {
                _target.ApplyDamage(_damage);
                NetworkObject.Despawn();
            }
        }
    }
}