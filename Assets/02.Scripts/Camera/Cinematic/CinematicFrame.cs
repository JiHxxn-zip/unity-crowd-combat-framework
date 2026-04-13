using System;
using UnityEngine;

namespace CrowdCombat.Camera.Cinematic
{
    public enum EaseType { Linear, EaseIn, EaseOut, EaseInOut }

    /// <summary>
    /// 시네마틱 단일 키프레임.
    /// position/rotation을 float 분리 저장 → JsonUtility 직렬화 호환.
    /// easeType은 "이 프레임 → 다음 프레임" 구간에 적용됩니다.
    /// </summary>
    [Serializable]
    public class CinematicFrame
    {
        public float time;

        // position
        public float px, py, pz;

        // eulerAngles (Quaternion 대신 사용 — JSON 가독성)
        public float rx, ry, rz;

        public float fov;

        public EaseType easeType;

        public Vector3 Position    => new Vector3(px, py, pz);
        public Quaternion Rotation => Quaternion.Euler(rx, ry, rz);

        public void SetPosition(Vector3 pos)
        {
            px = pos.x; py = pos.y; pz = pos.z;
        }

        public void SetRotation(Quaternion rot)
        {
            Vector3 e = rot.eulerAngles;
            rx = e.x; ry = e.y; rz = e.z;
        }
    }
}
