#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Sprites;
using TMPro;
using Jeomseon.Helper;

namespace Jeomseon.Editor.Tool
{
    internal sealed class IconCreator : EditorWindow
    {
        [MenuItem("Jeomseon/Icon Creator")]
        private static void Init()
        {
            GetWindow<IconCreator>().Show();
        }

        private int _size = 128;
        private int _divideCount = 4;

        private readonly List<Sprite> _iconSources = new();
        private ReorderableList _reorderableIconSources;
        private Texture2D _iconTexture = null;

        private void OnEnable()
        {
            _reorderableIconSources = new(
                _iconSources,
                typeof(Sprite),
                true,
                true,
                true,
                true)
            {
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.y += 2;
                    _iconSources[index] = (Sprite)EditorGUI.ObjectField(
                        new(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        _iconSources[index], typeof(Sprite), false);
                },
                onAddCallback = _ =>
                {
                    if (_iconSources.Count >= _divideCount * _divideCount) return;
                    _iconSources.Add(null);
                },
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Icon Sources")
            };
        }

        public void OnGUI()
        {
            _size = EditorGUILayout.IntSlider(new GUIContent("Size"), _size, 128, 2048);
            _divideCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Divide Count"), _divideCount), 1, 32);

            bool isCreate = GUILayout.Button("Create Icon");
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight * 3f);

            if (_iconTexture)
            {
                GUILayout.Label(_iconTexture);
            }

            _reorderableIconSources.DoLayoutList();

            if (isCreate)
            {
                int sizeMax = _size * _divideCount;
                int rows = (_iconSources.Count + _divideCount - 1) / _divideCount;
                int atlasWidth = Mathf.Min(_size * _divideCount, sizeMax);
                int atlasHeight = rows * _size;

                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Atlas",
                    "NewAtlas",
                    "png",
                    "Please enter a file name to save the atlas texture to");

                if (!string.IsNullOrEmpty(path))
                {
                    byte[] pngData = _iconTexture.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path, pngData);
                    AssetDatabase.Refresh();

                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Multiple;

                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                        // TMP_SpriteAsset 생성
                        Texture2D atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (!atlasTexture)
                        {
                            Debug.LogError("Failed to load the atlas texture.");
                            return;
                        }

                        // SpriteMetaData 설정
                        List<SpriteRect> metas = new();
                        int currentX = 0, currentY = 0;
                        foreach (Sprite sprite in _iconSources)
                        {
                            if (currentX + _size > atlasWidth)
                            {
                                currentX = 0;
                                currentY += _size;
                            }

                            SpriteRect meta = new SpriteRect
                            {
                                name = sprite.name,
                                rect = new(currentX, atlasHeight - currentY - _size, _size, _size),
                                alignment = SpriteAlignment.Center,
                                pivot = new(0.5f, 0.5f)
                            };
                            metas.Add(meta);
                            currentX += _size;
                        }

                        // .. TODO : 수정 필요
                        SpriteDataProviderFactories factory = new();
                        factory.Init();
                        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
                        dataProvider.InitSpriteEditorDataProvider();
                        dataProvider.SetSpriteRects(metas.ToArray());
                        dataProvider.Apply();
                        importer.SaveAndReimport();

                        // 스프라이트 아틀라스에서 스프라이트 목록을 가져오기
                        Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().ToArray();
                        if (sprites.Length == 0)
                        {
                            Debug.LogError("No sprites found in the atlas.");
                            return;
                        }

                        // TMP_SpriteAsset 생성
                        TMP_SpriteAsset spriteAsset = CreateInstance<TMP_SpriteAsset>();
                        spriteAsset.spriteSheet = atlasTexture;
                        spriteAsset.spriteInfoList = new();

                        foreach (Sprite sprite in sprites)
                        {
                            TMP_Sprite tmpSprite = new TMP_Sprite
                            {
                                id = spriteAsset.spriteInfoList.Count,
                                name = sprite.name,
                                x = sprite.rect.x,
                                y = sprite.rect.y,
                                width = sprite.rect.width,
                                height = sprite.rect.height,
                                pivot = sprite.pivot,
                                sprite = sprite
                            };

                            spriteAsset.spriteInfoList.Add(tmpSprite);
                        }

                        // TMP_SpriteAsset 저장
                        string assetPath = System.IO.Path.ChangeExtension(path, ".asset");
                        AssetDatabase.CreateAsset(spriteAsset, assetPath);
                        AssetDatabase.SaveAssets();

                        Debug.Log("TMP_SpriteAsset created at: " + assetPath);
                    }
                }
            }

            if (GUILayout.Button("Preview"))
            {
                int sizeMax = _size * _divideCount;
                int rows = (_iconSources.Count + _divideCount - 1) / _divideCount;
                int atlasWidth = Mathf.Min(_size * _iconSources.Count, sizeMax);
                int atlasHeight = rows * _size;

                Texture2D atlasTexture = new(atlasWidth, atlasHeight, TextureFormat.RGBA32, false);

                Color[][] resizeSourceColors = _iconSources
                    .Select(sprite =>
                    {
                        if (!sprite) return null;

                        int width = sprite.texture.width;
                        int height = sprite.texture.height;

                        RenderTexture renderTexture = new(width, height, 0)
                        {
                            useMipMap = false,
                            autoGenerateMips = false
                        };

                        RenderTexture.active = renderTexture;
                        Graphics.Blit(sprite.texture, renderTexture);

                        Texture2D readableTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                        readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        readableTexture.Apply();

                        RenderTexture.active = null;
                        renderTexture.Release();

                        return TextureHelper.ResizeColorPixel(
                            readableTexture.GetPixels(0, 0, width, height),
                            width,
                            height,
                            _size,
                            _size);
                    })
                    .Where(pixels => pixels is not null)
                    .ToArray();

                int currentX = 0, currentY = 0;
                foreach (Color[] t in resizeSourceColors)
                {
                    if (currentX + _size > atlasTexture.width)
                    {
                        currentX = 0;
                        currentY += _size;
                    }

                    if (currentY + _size > atlasTexture.height)
                    {
                        Debug.LogWarning("아틀라스 텍스처의 크기를 초과하여 더 이상 스프라이트를 추가할 수 없습니다.");
                        break;
                    }

                    atlasTexture.SetPixels(currentX, currentY, _size, _size, t);
                    currentX += _size;
                }

                if (currentX < atlasTexture.width || currentY + _size < atlasTexture.height)
                {
                    int remainingWidth = atlasTexture.width - currentX;
                    int remainingHeight = _size; // 마지막 줄의 높이만큼만 투명하게 채움

                    if (remainingWidth > 0)
                    {
                        Color[] transparentPixels = Enumerable.Repeat(new Color(0, 0, 0, 0), remainingWidth * remainingHeight).ToArray();
                        atlasTexture.SetPixels(currentX, currentY, remainingWidth, remainingHeight, transparentPixels);
                    }

                    if (currentY + _size < atlasTexture.height)
                    {
                        remainingHeight = atlasTexture.height - (currentY + _size);
                        Color[] transparentPixels = Enumerable.Repeat(new Color(0, 0, 0, 0), atlasTexture.width * remainingHeight).ToArray();
                        atlasTexture.SetPixels(0, currentY + _size, atlasTexture.width, remainingHeight, transparentPixels);
                    }
                }

                atlasTexture.Apply();
                _iconTexture = atlasTexture;
            }
        }
    }
}
#endif