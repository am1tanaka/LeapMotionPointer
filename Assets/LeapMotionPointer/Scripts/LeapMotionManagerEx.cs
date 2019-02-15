using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AM1.LeapMotionPointer
{
    public class LeapMotionManagerEx : HandModelManager
    {
        public static LeapMotionManagerEx leapMotionManagerEx { get; private set; }

        #region Serialize Fields

        [Header("操作用の値に変換するための設定")]
        [Tooltip("スクリーン座標に変換する対象のカメラ。未設定の場合はCamera.mainを利用。Camera.mainもnullの場合はスクリーン座標への変換はしません。"), SerializeField]
        Camera targetCamera;
        [Tooltip("左右読み取り有効幅"), SerializeField, Range(0, 1)]
        float visibleSize = 0.15f;
        [Tooltip("読み取り下端"), SerializeField, Range(0, 1)]
        float visibleUnder = 0.25f;
        [Tooltip("クリック閾値"), SerializeField, Range(0, 1)]
        float clickThreshold = 0.03f;
        [Tooltip("傾き限界値。この値をZ軸回転が越えたら、クリックの判定はしません。"), SerializeField, Range(0, 1)]
        float bankLimit = 0.2f;
        [Tooltip("ビューポートの画面端のこの領域外ではクリックできません。"), SerializeField, Range(0, 1)]
        float viewportClickLimit = 0f;
        [Tooltip("移動を平易化させる割合。"), SerializeField, Range(0, 1)]
        float flatRate = 0.33f;

        #endregion Serialize Fields

        #region Properties

        /// <summary>
        /// LeapMotionが使える時、trueを返します。
        /// </summary>
        public static bool isEnable
        {
            get
            {
                return (leapMotionManagerEx.leapProvider != null) && (clickState != CLICK_STATE.NONE);
            }
        }

        /// <summary>
        /// カーソルのビューポート座標系での座標
        /// </summary>
        public static Vector3 viewportPosition
        {
            get; private set;
        }
        /// <summary>
        /// 設定されているカメラのスクリーン座標
        /// </summary>
        public static Vector3 screenPosition
        {
            get; private set;
        }


        /// <summary>
        /// 今回、人差し指が下がった時、true。
        /// </summary>
        public static bool isPressDown { get; private set; }

        /// <summary>
        /// 人差し指が下がっている時、true。
        /// </summary>
        public static bool isPress { get { return clickState == CLICK_STATE.CLICK; } }

        /// <summary>
        /// 今回、人差し指が上がったら時、true
        /// </summary>
        public static bool isPressUp { get; private set; }

        /// <summary>
        /// 前回のクリック状態。クリック変化を確認するのに利用
        /// </summary>
        static bool isLastClick = false;

        /// <summary>
        /// 現在のクリックの状態
        /// </summary>
        static CLICK_STATE clickState;
        enum CLICK_STATE
        {
            NONE,               // 手を未検出
            STANDBY,            // 手は検出している
            WAIT_FIRSTFRAME,    // クリックして最初の1フレーム経過待ち
            CLICK               // 押し下げ
        }

        static PointerEventData pointerEventData;
        static List<RaycastResult> raycastResult;

        #endregion Properties

        #region Unity System

        private void Awake()
        {
            if (leapMotionManagerEx != null)
            {
                Destroy(gameObject);
                return;
            }

            leapMotionManagerEx = this;
        }

        private void Start()
        {
            pointerEventData = new PointerEventData(EventSystem.current);
            raycastResult = new List<RaycastResult>();

            isLastClick = false;
            clickState = CLICK_STATE.NONE;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

        }

        void Update()
        {
            isPressUp = isPressDown = false;

            if ((leapProvider == null)
                || (leapProvider.CurrentFixedFrame.Hands.Count == 0))
            {
                isLastClick = false;
                clickState = CLICK_STATE.NONE;
                return;
            }

            Hand hand = leapProvider.CurrentFixedFrame.Hands[0];

            // 中指の根元の座標
            Finger ringFinger = hand.Fingers[(int)Finger.FingerType.TYPE_RING];
            Vector3 fpos = ringFinger.Bone(Bone.BoneType.TYPE_METACARPAL).NextJoint.ToVector3();
            // yzのベクトルを利用
            //// 第1, 3象限は、yとzのベクトルの長さ
            //// 第2, 4象限は、y+zの値
            Vector3 yz = fpos;
            yz.x = 0f;
            yz.y -= visibleUnder;

            if ((yz.y >= 0) && (yz.z >= 0))
            {
                fpos.y = yz.magnitude;
            }
            else if ((yz.y < 0) && (yz.z < 0))
            {
                fpos.y = -yz.magnitude;
            }
            else
            {
                fpos.y = yz.y + yz.z;
            }

            fpos.z = 0f;
            fpos = fpos / visibleSize;
            if (clickState == CLICK_STATE.NONE)
            {
                // 先にカーソルが無効だったら、今回の座標にする
                viewportPosition = fpos;
            }
            else
            {
                // 前回も有効だった時は平均化
                viewportPosition = Vector3.Lerp(viewportPosition, fpos, flatRate);
            }

            // 人差し指と薬指の高さの差
            Bone indexBone = hand.Fingers[(int)Finger.FingerType.TYPE_INDEX].Bone(Bone.BoneType.TYPE_DISTAL);
            Vector3 indexPos = indexBone.NextJoint.ToVector3();
            Vector3 ringPos = ringFinger.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
            float clickDelta = indexPos.y - ringPos.y;

            // スクリーン座標に変換
            if (targetCamera != null)
            {
                screenPosition = targetCamera.ViewportToScreenPoint(viewportPosition);
            }

            // 手のバンク傾きが一定量を越えているか、ビューの外の時はクリックしない
            bool canClick = Mathf.Abs(hand.Rotation.z) < bankLimit;
            canClick &= ((fpos.x >= viewportClickLimit) && (fpos.x < 1f - viewportClickLimit));
            canClick &= ((fpos.y >= viewportClickLimit) && (fpos.y < 1f - viewportClickLimit));
            if (!canClick)
            {
                isPressUp = isPress;
                isLastClick = false;
                clickState = CLICK_STATE.STANDBY;
                return;
            }

            // クリックしている
            if (clickDelta < -clickThreshold)
            {
                if ((clickState == CLICK_STATE.NONE)
                    || (clickState == CLICK_STATE.STANDBY))
                {
                    clickState = CLICK_STATE.WAIT_FIRSTFRAME;
                }
                else
                {
                    clickState = CLICK_STATE.CLICK;

                    // 今回、初クリック
                    if (!isLastClick)
                    {
                        isPressDown = true;

                        // クリック場所のUIを調査
                        raycastResult.Clear();
                        pointerEventData.position = screenPosition;
                        EventSystem.current.RaycastAll(pointerEventData, raycastResult);
                        for (int i = 0; i < raycastResult.Count; i++)
                        {
                            Button btn = raycastResult[i].gameObject.GetComponent<Button>();
                            if (btn == null)
                            {
                                btn = raycastResult[i].gameObject.GetComponentInChildren<Button>();
                            }
                            if (btn != null)
                            {
                                btn.onClick.Invoke();
                                break;
                            }
                        }
                    }

                    isLastClick = true;
                }
            }
            // クリックしてない
            else
            {
                clickState = CLICK_STATE.STANDBY;
                isLastClick = false;
            }
        }

        #endregion Unity System

    }
}
