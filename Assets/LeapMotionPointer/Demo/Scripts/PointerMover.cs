using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AM1.LeapMotionPointer.Demo
{
    public class PointerMover : MonoBehaviour
    {
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

            // クリック時にポインターの経過時間をリセット
            if (LeapMotionManagerEx.isPressDown)
            {
                clickTime = 0;
            }
            // 押していたらSphereを運ぶ
            if (LeapMotionManagerEx.isPress)
            {
                myMaterial.color = Color.Lerp(clickColor, halfColor, clickTime);
                clickTime += Time.deltaTime;

                Vector3 contactPoint;
                GameObject go = getPointedGameObject(out contactPoint);
                Rigidbody rb = null;
                if (go != null)
                {
                    rb = go.GetComponent<Rigidbody>();
                }

                if (rb != null)
                {
                    Vector3 move = contactPoint - go.transform.position;
                    move.z = 0;
                    rb.velocity = move / Time.deltaTime;
                }
            }
            else
            {
                // 押してなかったら色を戻す
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