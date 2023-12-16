using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

public class TimelinePreviewerBehaviour : PlayableBehaviour
{
    public GameObject Prefab;
    public Transform Parent;
    public Vector3 Rotation;
    public Vector3 Position;
    public Vector3 Scale=Vector3.one;    
    public float ParticleTimeOffset;


    private GameObject _obj;
    private ParticleSystem _particleSystem;
    public float TimeScale { get; set; }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        base.OnBehaviourPlay(playable, info);
        if (Prefab == null) return;
        if (Parent == null) return;
        if (_obj != null)
        {
#if UNITY_EDITOR
            Object.DestroyImmediate(_obj);
#else
            Object.Destroy(_obj);
#endif
        }

        _obj = Object.Instantiate(Prefab, Parent);
        _obj.gameObject.SetActive(true);
        _particleSystem = _obj.GetComponent<ParticleSystem>();
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        base.OnBehaviourPause(playable, info);

#if UNITY_EDITOR
        Object.DestroyImmediate(_obj);
#else
            Object.Destroy(_obj);
#endif
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (_obj != null)
        {
            _obj.transform.localRotation = Quaternion.Euler(Rotation);
            _obj.transform.localPosition = Position;
            _obj.transform.localScale = Scale;
        }

        if (_particleSystem != null)
        {
            double time = playable.GetTime();
            _particleSystem.Play();
            _particleSystem.Simulate((float)(time+ParticleTimeOffset)*TimeScale, true);
            SceneView.RepaintAll();

        }
    }
}