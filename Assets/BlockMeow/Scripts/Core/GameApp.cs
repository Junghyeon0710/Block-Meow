using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlockMeow
{
    /// <summary>
    /// Entry point (the only component in the scene). Loads the profile, generates art and audio,
    /// builds the UI and routes between the lobby and the puzzle.
    /// </summary>
    public sealed class GameApp : MonoBehaviour
    {
        public static GameApp I { get; private set; }
        public const string Version = "1.0.0";
        public static string BootReport { get; private set; } = "";

        Camera _cam;

        IEnumerator Start()
        {
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Profile.Load();
            SetupCamera();
            var ui = UIRoot.Create();
            yield return null;

            float t0 = Time.realtimeSinceStartup;
            ui.SetSplash("블록을 말랑하게 굽는 중...");
            yield return Atlas.Build();
            float t1 = Time.realtimeSinceStartup;
            ui.SetSplash("냥이 목소리를 녹음하는 중...");
            var audio = gameObject.AddComponent<AudioManager>();
            yield return audio.Init();
            float t2 = Time.realtimeSinceStartup;
            BootReport = $"atlas {(t1 - t0) * 1000f:0} ms (cached={Atlas.FromCache}), sfx {(t2 - t1) * 1000f:0} ms";
            Debug.Log("[BlockMeow] boot: " + BootReport);

            Ads.Provider = ui;
            var session = new GameObject("GameSession").AddComponent<GameSession>();
            session.Init(_cam);
            ui.HideSplash();
            Track.Log("app_start", "games=" + Profile.D.gamesPlayed);

            if (!Profile.D.tutorialDone) session.StartTutorial();
            else GoHome();
        }

        void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                _cam = go.AddComponent<Camera>();
            }
            // without a listener every sound and the music would be silent
            if (FindAnyObjectByType<AudioListener>() == null) _cam.gameObject.AddComponent<AudioListener>();
            _cam.orthographic = true;
            _cam.orthographicSize = 8f;
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Pal.Bg;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 50f;

            // the notebook look is flat paper: no bloom (it would wash the white page out) and no vignette
            var data = _cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = false;
        }

        public void GoHome()
        {
            if (GameSession.I != null) GameSession.I.SetWorldVisible(false);
            UIRoot.I.ShowHome();
        }

        void Update() => Profile.Tick();

        void OnApplicationPause(bool paused)
        {
            if (paused) Profile.Save();
        }

        void OnApplicationQuit()
        {
            if (GameSession.I != null) GameSession.I.SaveClassic();
            Profile.Save();
        }
    }
}
