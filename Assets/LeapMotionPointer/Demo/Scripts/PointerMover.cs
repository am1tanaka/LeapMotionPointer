using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AM1.LeapMotionPointer.Demo
{
    public class PointerMover : MonoBehaviour
    {
        [Tooltip("クリック時にボールに加える力"), SerializeField]
        float addPower = 100f;
        [Tooltip("クリック時の色"), SerializeField]
        Color clickColor = Color.red;

        Color defaultColor;
        Color halfColor;
        Material myMaterial;
        float clickTime;

        private void Awake()
        {
            myMaterial = GetComponent<Image>().material;
            defaultColor = myMaterial.color;
            halfColor = Color.Lerp(clickColor, defaultColor, 0.5f);
        }

        void Update()
        {
            if (!LeapMotionManagerEx.isEnable) return;

            transform.position = LeapMotionManagerEx.screenPosition;
            Vector3 contactPoint;
            GameObject go = getPointedGameObject(out contactPoint);
            Rigidbody rb = null;
            if (go != null)
            {
                rb = go.GetComponent<Rigidbody>();
            }

            // クリック
            if (LeapMotionManagerEx.isPressDown)
            {
                clickTime = 0;
                myMaterial.color = clickColor;

                if ((go != null) && (rb != null))
                {
                    rb.AddForceAtPosition(Camera.main.transform.forward * addPower, contactPoint);
                }
            }
            else if (LeapMotionManagerEx.isPress)
            {
                clickTime += Time.deltaTime;
                myMaterial.color = Color.Lerp(clickColor, halfColor, clickTime);

                if ((go != null) && (rb != null))
                {
                    Vector3 move = contactPoint - go.transform.position;
                    move.z = 0;
                    rb.velocity = move / Time.deltaTime;
                }
            }
            else
            {
                myMaterial.color = defaultColor;
            }
        }

        /// <summary>
        /// カーソルが指している場所にあるゲームオブジェクトを一つ返します。
        /// </summary>
        /// <param name="cp">接触した座標を返す先のVector3</param>
        /// <returns>見つけたゲームオブジェクト。ない場合はnull</returns>
        GameObject getPointedGameObject(out Vector3 cp)
        {
            Ray ray = Camera.main.ScreenPointToRay(LeapMotionManagerEx.screenPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                cp = hit.point;
                return hit.collider.gameObject;
            }

            cp = ray.origin;
            return null;
        }
    }
}