using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AM1.LeapMotionPointer.Demo
{
    public class ClickCounter : MonoBehaviour
    {
        int counter = 0;
        Text counterText;

        void Start()
        {
            counterText = GetComponent<Text>();
        }

        public void OnClick()
        {
            counter++;
            counterText.text = counter + " Clicked";
        }

    }
}