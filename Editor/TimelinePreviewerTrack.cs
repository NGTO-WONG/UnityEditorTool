using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.855f, 0.8623f, 0.87f)]
[TrackClipType(typeof(TimelinePreviewerClip))]
[DisplayName("粒子特效预览轨道")]
public class TimelinePreviewerTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<TimelinePreviewerBehaviour>.Create(graph, inputCount);
    }
}