using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jeomseon.Helper
{
    public static class TextureHelper
    {
        public static Color[] ResizeColorPixel(Color[] originalPixels, int originalWidth, int originalHeight, int targetWidth, int targetHeight)
        {
            Color[] newPixels = new Color[targetWidth * targetHeight];

            float aspectRatio = (float)originalWidth / originalHeight;
            int resizeWidth, resizeHeight;

            if (aspectRatio > 1)
            {
                resizeWidth = targetWidth;
                resizeHeight = Mathf.RoundToInt(targetWidth / aspectRatio);
            }
            else
            {
                resizeHeight = targetHeight;
                resizeWidth = Mathf.RoundToInt(targetHeight * aspectRatio);
            }

            float ratioX = (float)originalWidth / resizeWidth;
            float ratioY = (float)originalHeight / resizeHeight;

            for (int y = 0; y < resizeHeight; y++)
            {
                for (int x = 0; x < resizeWidth; x++)
                {
                    int xFloor = Mathf.Clamp(Mathf.FloorToInt(x * ratioX), 0, originalWidth - 1);
                    int yFloor = Mathf.Clamp(Mathf.FloorToInt(y * ratioY), 0, originalHeight - 1);

                    newPixels[y * resizeWidth + x] = originalPixels[yFloor * originalWidth + xFloor];
                }
            }

            Color[] fullSizePixels = new Color[targetWidth * targetHeight];
            for (int i = 0; i < fullSizePixels.Length; i++)
            {
                fullSizePixels[i] = new(0, 0, 0, 0);
            }

            int xOffset = (targetWidth - resizeWidth) / 2;
            int yOffset = (targetHeight - resizeHeight) / 2;

            for (int y = 0; y < resizeHeight; y++)
            {
                for (int x = 0; x < resizeWidth; x++)
                {
                    fullSizePixels[(y + yOffset) * targetWidth + (x + xOffset)] = newPixels[y * resizeWidth + x];
                }
            }

            return fullSizePixels;
        }
    }
}