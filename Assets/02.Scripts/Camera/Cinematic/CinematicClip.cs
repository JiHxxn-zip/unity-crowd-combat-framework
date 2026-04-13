using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowdCombat.Camera.Cinematic
{
    public enum SpaceMode { World, Local }

    /// <summary>
    /// 시네마틱 클립. 키프레임 리스트 + 메타데이터를 담고 JSON으로 직렬화합니다.
    /// targetName은 에디터 식별용 메모이며, 런타임에서는 사용하지 않습니다.
    /// </summary>
    [Serializable]
    public class CinematicClip
    {
        public string clipName;
        public SpaceMode spaceMode;
        public string targetName;   // 에디터 메모 전용 — 런타임 미사용
        public List<CinematicFrame> frames = new();

        /// <summary>마지막 프레임의 time 값 (= 전체 재생 시간)</summary>
        public float Duration => frames is { Count: > 0 } ? frames[^1].time : 0f;

        public string ToJson() => JsonUtility.ToJson(this, true);

        public static CinematicClip FromJson(string json) => JsonUtility.FromJson<CinematicClip>(json);
    }
}
