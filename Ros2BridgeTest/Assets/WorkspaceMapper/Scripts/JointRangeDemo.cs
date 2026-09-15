using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WorkspaceMapper.Scripts
{
    public class JointRangeDemo : MonoBehaviour
    {
        [SerializeField] WorkspaceRobotBinding binding;
        [SerializeField] float secondsPerMove = 1.2f;
        [SerializeField] bool stopAtTable = true;
        [SerializeField] float toolLengthMm = 170f;

        [Button(ButtonSizes.Large)]
        public void SweepFullExtent()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Play mode only. Turn OFF live ROS drive first."); return; }
            if (!binding) { Debug.LogWarning("Assign binding."); return; }
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Vector2[] lim = MyCobot320Fk.Limits;
            float[] a = new float[6];
            for (int j = 0; j < 6; j++)
            {
                yield return MoveJoint(a, j, Safe(a, j, lim[j].x));
                yield return MoveJoint(a, j, Safe(a, j, lim[j].y));
                yield return MoveJoint(a, j, 0f);
            }
        }

        float Safe(float[] a, int j, float target)
        {
            if (!stopAtTable) return target;
            float[] test = (float[])a.Clone();
            test[j] = target;
            return MyCobot320Fk.PosMm(MyCobot320Fk.TcpBaseMatrix(test, toolLengthMm)).z < 0f ? a[j] : target;
        }

        IEnumerator MoveJoint(float[] a, int j, float target)
        {
            float start = a[j], t = 0f;
            while (t < secondsPerMove)
            {
                t += Time.deltaTime;
                a[j] = Mathf.Lerp(start, target, t / secondsPerMove);
                binding.DriveRealAngles(a);
                yield return null;
            }
            a[j] = target;
            binding.DriveRealAngles(a);
        }
    }
}