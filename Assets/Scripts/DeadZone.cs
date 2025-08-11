using System;
using UnityEngine;
using UnityEngine.Events;

public class DeadZone : MonoBehaviour
{
    [SerializeField] UnityEvent m_DeadEvent;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Player")
        {
            m_DeadEvent.Invoke();
        }
    }
}
