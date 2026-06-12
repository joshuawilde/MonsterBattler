using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// A one-shot golden spark explosion for UI (move unlocked, big rewards). Sits on a GameObject
    /// with a ParticleSystem + Coffee.UIExtensions.UIParticle (so it renders inside the Canvas).
    /// The particle "recipe" is configured here in code so it's reviewable/tunable: a radial burst
    /// of soft-circle sparks that shrink (size-over-lifetime) and fade gold→orange (color-over-
    /// lifetime), with a slight gravity arc. Units are canvas pixels (UIParticle.scale = 1).
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class UnlockBurst : MonoBehaviour
    {
        [Tooltip("Sparks in the burst.")] public int count = 38;
        [Tooltip("Spark speed range, canvas px/sec.")] public Vector2 speed = new(260f, 560f);
        [Tooltip("Spark size range, canvas px.")] public Vector2 size = new(14f, 34f);

        ParticleSystem _ps;

        void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            Configure();
        }

        public void Play()
        {
            if (_ps == null) { _ps = GetComponent<ParticleSystem>(); Configure(); }
            _ps.Clear();
            _ps.Play();
        }

        void Configure()
        {
            // Duration can only be set while fully stopped (Unity asserts otherwise).
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 28f;            // slight arc so sparks feel weighty
            main.maxParticles = 128;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var em = _ps.emission;                 // single instantaneous burst, no continuous rate
            em.enabled = true;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = _ps.shape;                 // emit radially from a small disc
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 8f;

            var sol = _ps.sizeOverLifetime;        // sparks shrink to nothing
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var col = _ps.colorOverLifetime;       // gold → orange → gone
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.96f, 0.62f), 0f),
                    new GradientColorKey(new Color(1f, 0.76f, 0.22f), 0.45f),
                    new GradientColorKey(new Color(1f, 0.42f, 0.12f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.55f),
                    new GradientAlphaKey(0f, 1f),
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            // Soft-circle sprite so sparks glow instead of rendering as hard quads.
            var r = GetComponent<ParticleSystemRenderer>();
            if (r != null && r.sharedMaterial == null)
            {
                var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = SoftCircle() };
                r.material = mat;
            }

            var ui = GetComponent<Coffee.UIExtensions.UIParticle>();
            if (ui != null) ui.scale = 1f;         // 1 canvas px per particle unit
        }

        static Texture2D _soft;
        static Texture2D SoftCircle()
        {
            if (_soft != null) return _soft;
            const int N = 64;
            _soft = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var c = new Vector2(N / 2f - 0.5f, N / 2f - 0.5f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / (N / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    _soft.SetPixel(x, y, new Color(1f, 1f, 1f, a * a)); // quadratic falloff = soft glow
                }
            _soft.Apply();
            return _soft;
        }
    }
}
