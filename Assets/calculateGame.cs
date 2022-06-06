using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class calculateGame : MonoBehaviour
{
    [SerializeField] ComputeShader _computeShader;

    [SerializeField] RenderTexture _renderTexture;
    [SerializeField] int _width;
    [SerializeField] int _height;
    [SerializeField] Color[] colors;
    [SerializeField] Material mat;
    Color _cachedWallColor;
    Color _cachedEmptyColor;
    [SerializeField, Range(0, 1000)] int _seed;
    int _cachedSeed;
    public int _Gap = -1;
    private Vector4[] colorsFloat;
    public bool useInitialImage;
    public Texture2D _InitialImage;
    public int colorsNum;
    public float sensitivity = 0.5f;

    bool compareColors(Color a, Color b, float error)
    {
        return (Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b)) < error;
    }

    void Start()
    {
        if (useInitialImage)
        {
            _width = _InitialImage.width;
            _height = _InitialImage.height;
            Texture2D copyTexture = new Texture2D(_width, _height);
            copyTexture.SetPixels(_InitialImage.GetPixels());
            copyTexture.Apply();
            _InitialImage = copyTexture;
            colors = new Color[256];
            colorsNum = 0;
            Color[] map = _InitialImage.GetPixels(0, 0, _width, _height);
            int max = _InitialImage.width * _InitialImage.height - 1;
            for (int j = 0; j < 512; j++)
            {
                int rand = Random.Range(0, max);
                Color col = map[rand];
                int i;
                for (i = 0; i < colorsNum; i++)
                {
                    if (compareColors(colors[i], col, 0.9f))
                    {
                        map[rand] = colors[i];
                        break;
                    }
                }
                if (i == colorsNum)
                {
                    Debug.Log("never seen this one before (random)");
                    colors[i] = col;
                    colorsNum++;
                }
            }
            for (int x = 0; x < _InitialImage.width; x++)
            {
                for (int y = 0; y < _InitialImage.height; y++)
                {
                    Color col = map[x+y*_width];
                    int i;
                    for (i = 0; i < colorsNum; i++)
                    {
                        if(compareColors(colors[i], col, sensitivity))
                        {
                            map[x+y*_width] = colors[i];
                            break;
                        }
                    }
                    if(i == colorsNum)
                    {
                        Debug.Log("never seen this one before");
                        colors[i] = col;
                        colorsNum++;
                    }
                }
            }
            _InitialImage.SetPixels(map, 0);
            _InitialImage.Apply();
        }
        int main = _computeShader.FindKernel("CSMain");
        _computeShader.SetTexture(main, "_InitialImage", _InitialImage);
        _renderTexture = new RenderTexture(_width, _height, 24);
        _renderTexture.filterMode = FilterMode.Point;
        _renderTexture.enableRandomWrite = true;
        _renderTexture.Create();

        //GetComponent<MeshRenderer>().material.mainTexture = _renderTexture;
        //GetComponent<MeshRenderer>().material.SetTexture("_MetallicGlossMap", _renderTexture);
        //GetComponent<MeshRenderer>().material.SetTexture("_ParallaxMap", _renderTexture);
        mat.mainTexture = _renderTexture;


        colorsFloat = new Vector4[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colorsFloat[i] = new Vector4(colors[i].r, colors[i].g, colors[i].b, colors[i].a);
        }

        _computeShader.SetInt("_Width", _renderTexture.width);
        _computeShader.SetInt("_Height", _renderTexture.height);
        _computeShader.SetInt("_Seed", _seed);
        _computeShader.SetInt("_ColorsNumber", colorsNum);
        _computeShader.SetBool("_UseInitialImage", useInitialImage);
        _computeShader.SetVectorArray("_Colors", colorsFloat);


        int prepass = _computeShader.FindKernel("CSInit");
        _computeShader.SetTexture(prepass, "_InitialImage", _InitialImage);
        _computeShader.SetTexture(prepass, "Result", _renderTexture);
        _computeShader.Dispatch(prepass, _renderTexture.width / 8, _renderTexture.height / 8, 1);
    }

    private void FixedUpdate()
    {
        GenerateMaze();
    }

    void GenerateMaze()
    {
        int main = _computeShader.FindKernel("CSMain");
        _computeShader.SetTexture(main, "Result", _renderTexture);
        _computeShader.SetInt("_Gap", _Gap);
        _computeShader.Dispatch(main, _renderTexture.width / 8, _renderTexture.height / 8, 1);
        _cachedSeed = _seed;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(_renderTexture, dest);
    }
}
