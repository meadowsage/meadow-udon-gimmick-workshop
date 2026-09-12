/*
MIT License

Copyright (c) 2025 Mashiro Small Theater

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace MashiroTheater
{
    /// <summary>
    /// ローカルプレイヤーのエリア進入を起点に、全員から見えるたらいを頭上へ落とす。
    /// たらいの所有者だけが物理演算と衝突通知を担当し、二重発動を防ぐ。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [AddComponentMenu("MashiroTheater/Mashiro Tarai Drop")]
    public class MashiroTaraiDrop : UdonSharpBehaviour
    {
        [Header("たらい設定")]
        [SerializeField, Tooltip("落下させるたらい。Rigidbody、Collider、VRC Object Sync を追加してください。")]
        private GameObject tarai;

        [SerializeField, Tooltip("たらいに付けた Rigidbody。")]
        private Rigidbody taraiRigidbody;

        [SerializeField, Tooltip("頭の位置からどれだけ上に出現させるか。")]
        private float heightAboveHead = 2f;

        [Header("衝突設定")]
        [SerializeField, Tooltip("衝突してから、たらいを非表示にするまでの秒数。")]
        private float hideDelay = 1f;

        [SerializeField, Tooltip("衝突時に全員へ再生する Audio Source。全員に聞こえるよう Spatial Blend を 0 にしてください。")]
        private AudioSource impactAudio;

        [UdonSynced] private bool isDropping;
        private bool collisionHandled;

        private void Start()
        {
            applyInactiveState();
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            // 各プレイヤーの入場は本人だけが要求し、全クライアントからの重複通知を避ける。
            if (player == null || !player.isLocal || isDropping || tarai == null || taraiRigidbody == null) return;

            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) return;

            Networking.SetOwner(localPlayer, gameObject);
            Networking.SetOwner(localPlayer, tarai);

            isDropping = true;
            collisionHandled = false;

            Vector3 headPosition = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            tarai.SetActive(true);
            tarai.transform.position = headPosition + (Vector3.up * heightAboveHead);
            tarai.transform.rotation = Quaternion.identity;
            taraiRigidbody.velocity = Vector3.zero;
            taraiRigidbody.angularVelocity = Vector3.zero;
            taraiRigidbody.isKinematic = false;

            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            // インスペクタの設定途中でも、同期受信によって例外が発生し続けないようにする。
            if (tarai == null) return;

            if (isDropping)
            {
                tarai.SetActive(true);
            }
            else
            {
                applyInactiveState();
            }
        }

        /// <summary>
        /// たらい側の衝突中継コンポーネントから、所有者だけが呼び出す。
        /// </summary>
        public void HandleTaraiCollision()
        {
            if (!isDropping || collisionHandled || tarai == null || !Networking.IsOwner(tarai)) return;

            collisionHandled = true;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayImpactAndScheduleHide));
        }

        /// <summary>
        /// 衝突音と非表示の時刻を全員でそろえるためのネットワークイベント。
        /// </summary>
        public void PlayImpactAndScheduleHide()
        {
            if (impactAudio != null)
            {
                impactAudio.Play();
            }

            SendCustomEventDelayedSeconds(nameof(HideTarai), Mathf.Max(0f, hideDelay));
        }

        /// <summary>
        /// 衝突後のたらいを片付け、所有者は次の入場を受け付けられる状態を同期する。
        /// </summary>
        public void HideTarai()
        {
            applyInactiveState();

            if (!Networking.IsOwner(gameObject)) return;

            isDropping = false;
            collisionHandled = false;
            RequestSerialization();
        }

        private void applyInactiveState()
        {
            if (taraiRigidbody != null)
            {
                taraiRigidbody.velocity = Vector3.zero;
                taraiRigidbody.angularVelocity = Vector3.zero;
                taraiRigidbody.isKinematic = true;
            }

            if (tarai != null)
            {
                tarai.SetActive(false);
            }
        }
    }
}
