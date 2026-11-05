using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed class PrototypeAudio
    {
        private readonly AudioSource source;
        private readonly Dictionary<EventKind, AudioClip> clips = new Dictionary<EventKind, AudioClip>();
        private readonly AudioClip heavyDeath, cannonShot, controlShot;
        private float lastShot, lastHunterShot, lastImpact, lastKill, lastHeavyKill;
        private int killChain;
        public bool Muted { get; set; }

        public PrototypeAudio(GameObject owner)
        {
            source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.volume = 0.25f;
            clips[EventKind.HunterShot] = Tone(720, 220, 0.06f);
            clips[EventKind.TowerShot] = Tone(280, 90, 0.075f);
            clips[EventKind.Hit] = Impact(0.09f, 0.30f);
            clips[EventKind.Kill] = Impact(0.085f, 0.55f);
            clips[EventKind.Build] = Tone(400, 1000, 0.3f);
            clips[EventKind.Upgrade] = Tone(550, 1200, 0.25f);
            clips[EventKind.Fusion] = Tone(240, 1400, 0.65f);
            clips[EventKind.Sale] = Tone(900, 400, 0.2f);
            clips[EventKind.Warning] = Tone(330, 440, 0.3f);
            clips[EventKind.CoreHurt] = Tone(140, 55, 0.18f);
            clips[EventKind.HunterHurt] = Tone(210, 80, 0.13f);
            clips[EventKind.Wave] = Tone(300, 600, 0.4f);
            clips[EventKind.End] = Tone(600, 240, 0.6f);
            heavyDeath = Impact(.21f, .80f);
            cannonShot = Impact(.11f, .70f);
            controlShot = Tone(660, 360, .085f);
        }
        public void Play(CombatEvent effect)
        {
            if (Muted) return;
            AudioClip clip;
            if (!clips.TryGetValue(effect.Kind, out clip)) return;
            bool shot = effect.Kind == EventKind.HunterShot || effect.Kind == EventKind.TowerShot;
            bool kill = effect.Kind == EventKind.Kill;
            bool impact = effect.Kind == EventKind.Hit;
            if (kill) {
                if (effect.Source >= (int)EnemyKind.Brute) {
                    if (Time.unscaledTime - lastHeavyKill < .10f) return;
                    lastHeavyKill = lastKill = Time.unscaledTime;
                    source.pitch = effect.Source == (int)EnemyKind.Boss ? .76f : .94f;
                    source.PlayOneShot(heavyDeath, .82f); return;
                }
                if (Time.unscaledTime - lastKill < .045f) return;
                killChain = Time.unscaledTime - lastKill < .32f ? Mathf.Min(5, killChain + 1) : 0;
                lastKill = Time.unscaledTime; source.pitch = 1.0f + killChain * .035f;
                source.PlayOneShot(clip, .62f); return;
            }
            source.pitch = 1;
            if (effect.Kind == EventKind.HunterShot) {
                if (Time.unscaledTime - lastHunterShot < .065f) return;
                lastHunterShot = Time.unscaledTime;
                source.PlayOneShot(clip, .52f); return;
            }
            if (shot && Time.unscaledTime - lastShot < .05f) return;
            if (impact && Time.unscaledTime - lastImpact < 0.055f) return;
            if (shot) {
                lastShot = Time.unscaledTime;
                TowerKind kind = (TowerKind)effect.Source;
                if (kind == TowerKind.Rupture || kind == TowerKind.Shatter || kind == TowerKind.Seal) clip = cannonShot;
                else if (kind == TowerKind.Binding || kind == TowerKind.Marker || kind == TowerKind.Beacon) clip = controlShot;
            }
            if (impact) lastImpact = Time.unscaledTime;
            source.PlayOneShot(clip, shot ? 0.35f : impact ? 0.4f : 0.65f);
        }
        public void Dispose()
        {
            if (source != null) source.Stop();
            foreach (AudioClip clip in clips.Values) if (clip != null) Object.Destroy(clip);
            Object.Destroy(heavyDeath); Object.Destroy(cannonShot); Object.Destroy(controlShot);
            if (source != null) Object.Destroy(source);
        }
        private static AudioClip Tone(float from, float to, float duration)
        {
            const int rate = 22050;
            float[] data = new float[(int)(rate * duration)];
            float phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / data.Length;
                phase += Mathf.Lerp(from, to, t) * 2 * Mathf.PI / rate;
                data[i] = Mathf.Sin(phase) * (1 - t) * Mathf.Min(1, t * 30) * 0.4f;
            }
            AudioClip clip = AudioClip.Create("Gatefall tone", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
        private static AudioClip Impact(float duration, float volume)
        {
            const int rate = 22050;
            float[] data = new float[(int)(rate * duration)];
            uint noise = 19873;
            float phase = 0, filtered = 0;
            for (int i = 0; i < data.Length; i++) {
                float t = (float)i / data.Length;
                noise = unchecked(noise * 1664525u + 1013904223u);
                float sample = ((noise >> 8) / 8388608f) - 1f;
                filtered = filtered * .7f + sample * .3f;
                phase += Mathf.Lerp(180, 80, t) * 2 * Mathf.PI / rate;
                data[i] = (Mathf.Sin(phase) * .45f + filtered * .55f) * (1 - t) * (1 - t) * volume;
            }
            AudioClip clip = AudioClip.Create("Gatefall impact", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
