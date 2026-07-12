using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "2D/Tiles/TileList")]
public class TileList : ScriptableObject
{
    public List<TileBase> Tiles;
}