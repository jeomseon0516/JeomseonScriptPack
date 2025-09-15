#if UNITY_EDITOR
using System;
using Jeomseon.Extensions;
using Jeomseon.Helper;
using UnityEngine;
using UnityEditor;
using Event = UnityEngine.Event;

namespace Jeomseon.Editor.GUI
{
    using Editor = UnityEditor.Editor;
    using GUI = UnityEngine.GUI;

    public sealed class SceneViewInnerWindow<T> where T : Editor
    {
        private const float WIDTH_MIN = 100f;
        private const float HEIGHT_MIN = 200f;

        public bool IsUse { get; private set; } = false;

        private static readonly string _windowOptionKey = typeof(T).Name + "WindowOption";
        private Rect _windowRect = new(40, 30, 200, 400);
        private bool _isResizingLeft = false;
        private bool _isResizingRight = false;
        private bool _isResizingTop = false;
        private bool _isResizingBottom = false;
        private GUIStyle _windowStyle = null;

        public void OnEnable()
        {
            if (EditorPrefs.HasKey(_windowOptionKey))
            {
                _windowRect = JsonUtility.FromJson<SerializedRect>(EditorPrefs.GetString(_windowOptionKey));
            }

            IsUse = false;
        }

        public void OnDisable()
        {
            EditorPrefs.SetString(_windowOptionKey, JsonUtility.ToJson(new SerializedRect(_windowRect)));
        }

        public void OnSceneGUI(Action<int> call)
        {
            IsUse = false;

            Handles.BeginGUI();

            if (_windowStyle is null)
            {
                _windowStyle = new(GUI.skin.window);
                _windowStyle.active = _windowStyle.normal;
                _windowStyle.focused = _windowStyle.normal;
                _windowStyle.onNormal = _windowStyle.normal;
                _windowStyle.onActive = _windowStyle.normal;
                _windowStyle.onFocused = _windowStyle.normal;
                _windowStyle.stretchWidth = true;
                _windowStyle.stretchHeight = true;
            }

            _windowRect = GUILayout.Window(0, _windowRect, id => drawWindowContents(id, call), "Tile Option", _windowStyle);

            handleWindowDragAndResize();

            if (_windowRect.x < 0)
            {
                _windowRect.x = 0;
            }

            if (_windowRect.xMax > SceneView.currentDrawingSceneView.cameraViewport.xMax)
            {
                _windowRect.x = SceneView.currentDrawingSceneView.cameraViewport.xMax - _windowRect.width;
            }

            if (_windowRect.y < 0)
            {
                _windowRect.y = 0;
            }

            if (_windowRect.yMax > SceneView.currentDrawingSceneView.cameraViewport.yMax)
            {
                _windowRect.y = SceneView.currentDrawingSceneView.cameraViewport.yMax - _windowRect.height;
            }

            Handles.EndGUI();
        }

        private void drawWindowContents(int windowId, Action<int> call)
        {
            call?.Invoke(windowId);
            GUI.DragWindow(new(0, 0, _windowRect.width, 20)); // 상단 20px 영역을 드래그 가능하도록 설정
        }

        private void handleWindowDragAndResize()
        {
            Event currentEvent = Event.current;
            Vector2 mousePosition = currentEvent.mousePosition;
            float toolbarHeight = SceneView.currentDrawingSceneView.rootVisualElement.GetRootVisualElement().contentRect.height - SceneView.currentDrawingSceneView.cameraViewport.height;

            Rect leftTopArea = new(_windowRect.xMin - 5, _windowRect.yMin - 5, 10, 10);
            Rect leftArea = new(_windowRect.xMin - 5, _windowRect.yMin + 5, 10, _windowRect.height - 10);
            Rect leftBottomArea = new(_windowRect.xMin - 5, _windowRect.yMax - 5, 10, 10);
            Rect bottomArea = new(_windowRect.xMin + 5, _windowRect.yMax - 5, _windowRect.width - 10, 10);
            Rect rightBottomArea = new(_windowRect.xMax - 5, _windowRect.yMax - 5, 10, 10);
            Rect rightArea = new(_windowRect.xMax - 5, _windowRect.yMin + 5, 10, _windowRect.height - 10);
            Rect topArea = new(_windowRect.xMin + 5, _windowRect.yMin - 5, _windowRect.width - 10, 10);
            Rect rightTopArea = new(_windowRect.xMax - 5, _windowRect.yMin - 5, 10, 10);

            setCursor(leftTopArea, MouseCursor.ResizeUpLeft);
            setCursor(leftArea, MouseCursor.ResizeHorizontal);
            setCursor(leftBottomArea, MouseCursor.ResizeUpRight);
            setCursor(bottomArea, MouseCursor.ResizeVertical);
            setCursor(rightBottomArea, MouseCursor.ResizeUpLeft);
            setCursor(rightArea, MouseCursor.ResizeHorizontal);
            setCursor(topArea, MouseCursor.ResizeVertical);
            setCursor(rightTopArea, MouseCursor.ResizeUpRight);

            if (currentEvent.button == 0)
            {
                switch (currentEvent.type)
                {
                    case EventType.MouseDown:
                        {
                            setUseByRectContains();

                            if (leftTopArea.Contains(mousePosition))
                            {
                                _isResizingLeft = true;
                                _isResizingTop = true;
                                setUse();
                            }

                            if (leftArea.Contains(mousePosition))
                            {
                                _isResizingLeft = true;
                                setUse();
                            }

                            if (leftBottomArea.Contains(mousePosition))
                            {
                                _isResizingLeft = true;
                                _isResizingBottom = true;
                                setUse();
                            }

                            if (bottomArea.Contains(mousePosition))
                            {
                                _isResizingBottom = true;
                                setUse();
                            }

                            if (rightBottomArea.Contains(mousePosition))
                            {
                                _isResizingBottom = true;
                                _isResizingRight = true;
                                setUse();
                            }

                            if (rightArea.Contains(mousePosition))
                            {
                                _isResizingRight = true;
                                setUse();
                            }

                            if (topArea.Contains(mousePosition))
                            {
                                _isResizingTop = true;
                                setUse();
                            }

                            if (rightTopArea.Contains(mousePosition))
                            {
                                _isResizingRight = true;
                                _isResizingTop = true;
                                setUse();
                            }
                            break;
                        }
                    case EventType.MouseDrag:
                        {
                            if (_isResizingLeft)
                            {
                                float deltaX = mousePosition.x - _windowRect.x;
                                _windowRect.x = mousePosition.x;
                                _windowRect.width -= deltaX;

                                // 왼쪽 경계와 최소 너비 확인
                                if (_windowRect.x < SceneView.currentDrawingSceneView.cameraViewport.xMin)
                                {
                                    _windowRect.width -= SceneView.currentDrawingSceneView.cameraViewport.xMin - _windowRect.x;
                                    _windowRect.x = SceneView.currentDrawingSceneView.cameraViewport.xMin;
                                }

                                if (_windowRect.width < WIDTH_MIN)
                                {
                                    _windowRect.width = WIDTH_MIN;
                                }

                                setUse();
                            }

                            if (_isResizingRight)
                            {
                                _windowRect.width = mousePosition.x - _windowRect.x;

                                if (_windowRect.xMax > SceneView.currentDrawingSceneView.cameraViewport.xMax)
                                {
                                    _windowRect.width = SceneView.currentDrawingSceneView.cameraViewport.xMax - _windowRect.x;
                                }

                                if (_windowRect.width < WIDTH_MIN)
                                {
                                    _windowRect.width = WIDTH_MIN;
                                }

                                setUse();
                            }

                            if (_isResizingTop)
                            {
                                float deltaY = mousePosition.y - _windowRect.y;
                                _windowRect.y = mousePosition.y;
                                _windowRect.height -= deltaY;

                                // 위쪽 경계와 최소 높이 확인
                                if (_windowRect.y < SceneView.currentDrawingSceneView.cameraViewport.yMin)
                                {
                                    _windowRect.height -= SceneView.currentDrawingSceneView.cameraViewport.yMin - _windowRect.y;
                                    _windowRect.y = SceneView.currentDrawingSceneView.cameraViewport.yMin;
                                }

                                if (_windowRect.height < HEIGHT_MIN)
                                {
                                    _windowRect.height = HEIGHT_MIN;
                                }

                                setUse();
                            }

                            if (_isResizingBottom)
                            {
                                _windowRect.height = mousePosition.y - _windowRect.y;

                                if (_windowRect.yMax > SceneView.currentDrawingSceneView.cameraViewport.yMax)
                                {
                                    _windowRect.height = SceneView.currentDrawingSceneView.cameraViewport.yMax - _windowRect.y;
                                }

                                if (_windowRect.height < HEIGHT_MIN)
                                {
                                    _windowRect.height = HEIGHT_MIN;
                                }

                                setUse();
                            }
                            break;
                        }
                    case EventType.MouseUp:
                        setUseByRectContains();
                        _isResizingLeft = false;
                        _isResizingRight = false;
                        _isResizingTop = false;
                        _isResizingBottom = false;
                        break;

                        void setUseByRectContains()
                        {
                            if (!_windowRect.Contains(mousePosition) &&
                                !leftTopArea.Contains(mousePosition) &&
                                !leftArea.Contains(mousePosition) &&
                                !leftBottomArea.Contains(mousePosition) &&
                                !bottomArea.Contains(mousePosition) &&
                                !rightBottomArea.Contains(mousePosition) &&
                                !rightArea.Contains(mousePosition) &&
                                !topArea.Contains(mousePosition) &&
                                !rightTopArea.Contains(mousePosition)) return;

                            setUse();
                        }
                }
            }

            void setUse()
            {
                currentEvent.Use();
                IsUse = true;
            }

            void setCursor(in Rect rect, in MouseCursor cursor)
            {
                EditorGUIUtility.AddCursorRect(new(rect.x, rect.y + toolbarHeight, rect.width, rect.height), cursor);
            }
        }
    }
}
#endif