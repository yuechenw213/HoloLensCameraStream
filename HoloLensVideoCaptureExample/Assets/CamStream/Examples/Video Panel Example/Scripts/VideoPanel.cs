//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class VideoPanel : MonoBehaviour
{
    public RawImage rawImage;
    public MyTcpClient tcpClient;

    private Texture2D texture;

    public void SetResolution(int width, int height)
    {
        texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
        //rawImage.texture = texture;
    }

    //private bool usingTexture = false;
    public void SetBytes(byte[] image)
    {
        //if(usingTexture)
        //{
        //    return;
        //}
        //usingTexture = true;
        //var texture = rawImage.texture as Texture2D;
        //texture.LoadRawTextureData(image); //TODO: Should be able to do this:
        //texture.LoadRawTextureData(pointerToImage, 1280 * 720 * 4);
        //texture.Apply();

        //Thread videoThread = new Thread(() => tcpClient.SendTexture(texture));
        //videoThread.Start();
        //tcpClient.SendTexture(texture);
        //tcpClient.SendEncodedImg(image);
        //usingTexture = false;
    }

    public void encodeImg(byte[] image, int width, int height)
    {


        byte[] bytes = ImageConversion.EncodeArrayToJPG(image, UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_SRGB, (uint)width, (uint)height);
        
        tcpClient.SendEncodedImg(bytes);
    }

    public byte[] transferImg(byte[] image)
    {
        int p = image.Length / 4;
        byte[] result = new byte[p * 3];
        for(int i = 0; i < p; i++)
        {
            for(int j = 0; j < 3; j++)
            {
                result[i * 3 + j] = image[i * 4 + j];
            }
        }
        return result;
    }
}
