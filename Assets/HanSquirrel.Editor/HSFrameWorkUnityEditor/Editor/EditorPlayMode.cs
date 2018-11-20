using UnityEditor;
using System;

//https://answers.unity.com/questions/447701/event-for-unity-editor-pause-and-playstop-events.html

namespace HSFrameWork.Common.Editor
{
    public enum PlayModeState
    {
        Stopped,
        Playing,
        Paused
    }

    /// <summary>
    /// 监控PlayMode的改变工具类
    /// </summary>
    [InitializeOnLoad]
    public class EditorPlayMode
    {
        private static PlayModeState _currentState = PlayModeState.Stopped;

        static EditorPlayMode()
        {
            EditorApplication.playmodeStateChanged = OnUnityPlayModeChanged;
            if (EditorApplication.isPaused)
                _currentState = PlayModeState.Paused;
        }

        public static event Action<PlayModeState, PlayModeState> PlayModeChanged;

        private static void OnPlayModeChanged(PlayModeState currentState, PlayModeState changedState)
        {
            if (PlayModeChanged != null)
                PlayModeChanged(currentState, changedState);
        }

        private static void OnUnityPlayModeChanged()
        {
            var changedState = PlayModeState.Stopped;
            switch (_currentState)
            {
                case PlayModeState.Stopped:
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        changedState = PlayModeState.Playing;
                    }
                    else if (EditorApplication.isPaused)
                    {
                        changedState = PlayModeState.Paused;
                    }
                    break;
                case PlayModeState.Playing:
                    if (EditorApplication.isPaused)
                    {
                        changedState = PlayModeState.Paused;
                    }
                    else if (EditorApplication.isPlaying)
                    {
                        changedState = PlayModeState.Playing;
                    }
                    else
                    {
                        changedState = PlayModeState.Stopped;
                    }
                    break;
                case PlayModeState.Paused:
                    if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused)
                    {
                        changedState = PlayModeState.Playing;
                    }
                    else if (EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPaused)
                    {
                        changedState = PlayModeState.Paused;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Fire PlayModeChanged event.
            if (_currentState != changedState)
                OnPlayModeChanged(_currentState, changedState);

            // Set current state.
            _currentState = changedState;
        }
    }
}
