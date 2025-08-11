using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Vehicles.Car;


[RequireComponent(typeof(CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use
        
        private Collider[] m_Colliders;


        private int defaultLayer = 0;
        private int noPhysicsLayer = 0;

        private void Awake()
        {
            
            // get the car controller
            m_Car = GetComponent<CarController>();
            defaultLayer = this.gameObject.layer;
            noPhysicsLayer = LayerMask.NameToLayer("NoPhysics"); // Ensure "NoPhysics" layer exists in U
            m_Colliders = gameObject.GetComponentsInChildren<Collider>();
        }


        private void FixedUpdate()
        {
            
            // ----> Fetch from YOUR singleton
            // InputManagerGeneral.Instance;
            //     
            // m_Car.Move(steering, accel, footbrake, 0f);
        }
        
        public IEnumerator TemporaryLayerChange(float duration)
        {
            // Change layer for the parent and all children
            for (int i = 0; i < m_Colliders.Length; i++)
            {
                m_Colliders[i].transform.gameObject.layer = noPhysicsLayer;
            }
            
            // Wait for specified duration
            yield return new WaitForSeconds(duration);
        
            // Revert layer for the parent and all children
            for (int i = 0; i < m_Colliders.Length; i++)
            {
                m_Colliders[i].transform.gameObject.layer = defaultLayer;
            }
        }
    }
