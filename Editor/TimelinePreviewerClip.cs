using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

[Serializable]
[DisplayName("粒子特效预览切片")]
public class TimelinePreviewerClip : PlayableAsset
{
    public ExposedReference<Transform> parent;
    public GameObject prefab;
    public Vector3 rotation;
    public Vector3 position;
    public Vector3 scale=Vector3.one;
    public float timeScale =1f;
    public float particleTimeOffset;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<TimelinePreviewerBehaviour>.Create(graph);
        TimelinePreviewerBehaviour behaviour = playable.GetBehaviour();
        behaviour.Parent = parent.Resolve(graph.GetResolver());
        behaviour.Prefab = prefab;
        behaviour.Rotation = rotation;
        behaviour.Position = position;
        behaviour.Scale = scale;
        behaviour.TimeScale = timeScale;
        behaviour.ParticleTimeOffset = particleTimeOffset;
        return playable;
    }
}