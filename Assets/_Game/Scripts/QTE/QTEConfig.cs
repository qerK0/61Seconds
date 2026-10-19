using SixtyOne.Audio;
using UnityEngine;

namespace SixtyOne.QTE
{
    /// <summary>Tuning for a WASD quick-time event (one asset per use: breaker wiring, dog evasion).</summary>
    [CreateAssetMenu(menuName = "SixtyOne/QTE Config", fileName = "QTEConfig")]
    public sealed class QTEConfig : ScriptableObject
    {
        [Tooltip("Shortest sequence (keys).")]
        [Min(1)]
        [SerializeField] private int minSequenceLength = 4;

        [Tooltip("Longest sequence (keys). Each attempt rolls a length in [min, max].")]
        [Min(1)]
        [SerializeField] private int maxSequenceLength = 6;

        [Tooltip("Seconds to press each key.")]
        [Min(0.1f)]
        [SerializeField] private float timePerKey = 1.2f;

        [Tooltip("Seconds for the whole sequence (0 = only the per-key window).")]
        [Min(0f)]
        [SerializeField] private float totalTimeout = 8f;

        [Tooltip("Building-timer seconds lost on each failed attempt.")]
        [Min(0f)]
        [SerializeField] private float failureBuildingTimerPenalty = 5f;

        [Tooltip("Seconds the QTE freezes on a miss (hit-stop; Time.timeScale is never touched).")]
        [Min(0f)]
        [SerializeField] private float hitStopSeconds = 0.15f;

        [Tooltip("Camera shake on a miss (world units).")]
        [Min(0f)]
        [SerializeField] private float failShake = 0.08f;

        [Tooltip("Played on a correct key.")]
        [SerializeField] private AudioCue hitCue;

        [Tooltip("Played on a miss.")]
        [SerializeField] private AudioCue missCue;

        [Tooltip("Played when the sequence is completed.")]
        [SerializeField] private AudioCue successCue;

        public int SequenceLengthMin => minSequenceLength;
        public int SequenceLengthMax => Mathf.Max(minSequenceLength, maxSequenceLength);
        public float TimePerKey => timePerKey;
        public float TotalTimeout => totalTimeout;
        public float FailureBuildingTimerPenalty => failureBuildingTimerPenalty;
        public float HitStopSeconds => hitStopSeconds;
        public float FailShake => failShake;
        public AudioCue HitCue => hitCue;
        public AudioCue MissCue => missCue;
        public AudioCue SuccessCue => successCue;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(int minLength, int maxLength, float perKey, float total, float penalty, AudioCue hit, AudioCue miss, AudioCue success)
        {
            minSequenceLength = minLength;
            maxSequenceLength = maxLength;
            timePerKey = perKey;
            totalTimeout = total;
            failureBuildingTimerPenalty = penalty;
            hitCue = hit;
            missCue = miss;
            successCue = success;
        }
    }
}
