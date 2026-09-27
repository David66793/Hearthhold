using UnityEngine;

namespace Hearthhold.UnityClient
{
    // A short, deterministic forged-metal/stone impact keeps this equipment audible without
    // shipping an unlicensed or stylistically mismatched third-party sound file.
    internal static class RiftHammerAudio
    {
        private static AudioClip impactClip;
        internal static float PeakAmplitude { get; private set; }

        internal static void PlayImpact(Transform parent, Vector3 position, int level)
        {
            if (parent == null) return;
            if (impactClip == null) impactClip = BuildImpactClip();
            GameObject sound = new GameObject("Rift hammer impact audio");
            sound.transform.SetParent(parent, false);
            sound.transform.position = position;
            AudioSource source = sound.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.3f;
            source.minDistance = 2.5f;
            source.maxDistance = 32f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.volume = 0.38f;
            source.pitch = 0.95f + Mathf.Clamp(level, 1, 3) * 0.025f;
            source.clip = impactClip;
            source.Play();
            Object.Destroy(sound, impactClip.length + 0.12f);
        }

        private static AudioClip BuildImpactClip()
        {
            const int rate = 22050;
            const float duration = 0.42f;
            int count = Mathf.CeilToInt(rate * duration);
            float[] samples = new float[count];
            uint noiseState = 0x9E3779B9u;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                noiseState ^= noiseState << 13;
                noiseState ^= noiseState >> 17;
                noiseState ^= noiseState << 5;
                float noise = (noiseState & 0xffffu) / 32768f - 1f;
                float strike = Mathf.Sin(2f * Mathf.PI * (155f * t - 105f * t * t)) * Mathf.Exp(-t * 13f);
                float metal = (Mathf.Sin(2f * Mathf.PI * 842f * t)
                    + 0.43f * Mathf.Sin(2f * Mathf.PI * 1286f * t)) * Mathf.Exp(-t * 22f);
                float grit = noise * Mathf.Exp(-t * 47f);
                float fadeIn = Mathf.Clamp01(t * 1400f);
                samples[i] = Mathf.Clamp((strike * 0.42f + metal * 0.25f + grit * 0.26f) * fadeIn, -0.95f, 0.95f);
                PeakAmplitude = Mathf.Max(PeakAmplitude, Mathf.Abs(samples[i]));
            }
            AudioClip clip = AudioClip.Create("RiftHammer forged wall impact", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
