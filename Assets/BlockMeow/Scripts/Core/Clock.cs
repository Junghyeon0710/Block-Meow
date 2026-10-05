using UnityEngine;

namespace BlockMeow
{
    /// <summary>
    /// Time step for animations. Menus, tweens and effects keep running while the game is paused, so they use
    /// unscaled time; while frames are being recorded (Time.captureDeltaTime) they step by the capture rate instead,
    /// so a slow capture still comes out at normal speed.
    /// </summary>
    public static class Clock
    {
        public static float Dt => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
    }
}
