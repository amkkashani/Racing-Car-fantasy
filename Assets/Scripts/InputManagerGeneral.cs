using System;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

namespace Racing2D
{
    public class InputManagerGeneral : SingletonMagic<InputManagerGeneral>
    {
        [SerializeField] private float steering;
        [SerializeField] private float accel;
        [SerializeField] private float footbrake;

        [SerializeField] private HoldButtonHandler gas;
        [SerializeField] private HoldButtonHandler brake;

        [SerializeField] private float touchWheel;

        [SerializeField] private ICar m_car;

        private void Start()
        {
            m_car = GetComponentInChildren<ICar>();
        }


        public float GetSteering()
        {
            return steering;
        }

        public float GetAccel()
        {
            return accel;
        }

        public float GetFootbrake()
        {
            return footbrake;
        }

        private void Update()
        {
            accel = (Input.GetKey(KeyCode.W) ? 1f : 0f);
            footbrake = Input.GetKey(KeyCode.S) ? -1f : 0f;

            accel += gas.isPressed ? 1f : 0f;
            footbrake += brake.isPressed ? -1f : 0f;

            // steering += touchWheel;

            if (touchWheel == 0)
            {
                //if no one touch any thing we will read data from keyboard
                steering = (Input.GetKey(KeyCode.A) ? -1f : 0f) +
                           (Input.GetKey(KeyCode.D) ? 1f : 0f);;
            }
            else
            {
                // if touch system works it will over ride the normal form
                steering = touchWheel;
            }
            
            m_car.Move(steering, accel, footbrake, 0f);
        }

        public void Setsteering(float steering)
        {
            this.steering = steering;
        }

        public void SetAccel(float accel)
        {
            this.accel = accel;
        }

        public void SetFootbrake(float footbrake)
        {
            this.footbrake = footbrake;
        }

        public void SetTouchWheel(float touchWheel)
        {
            this.touchWheel = touchWheel;
        }
    }
}