using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "uSrcResources", menuName = "uSource Resources Data", order = 1)]
public class uSrcResources : ScriptableObject
{
    [Header("Materials")]
    public Material diffuseMaterial;
    public Material transparentMaterial;
    public Material transparentCutout;

    [Header("Shaders")]
    public Shader sUnlit;
    public Shader sUnlitTransparent;
    public Material vertexLitMaterial;
    public Shader sSelfillum;
    public Shader sAdditive;
    public Shader sRefract;
    public Shader sWorldVertexTransition;
}
