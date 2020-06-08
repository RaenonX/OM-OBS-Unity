using System;
using System.Collections.Generic;
using UnityEngine;

namespace OM.OBS
{
    public class VoxelCharacter : MonoBehaviour
    {
        [SerializeField] public Vector2Int FontPoints;
        [SerializeField] public Texture2D FontTexture;
        [SerializeField] public Mesh VoxelMesh;
        [SerializeField] public Vector2 VoxelDistance;
        [SerializeField] public Material VoxelMaterial;
        [SerializeField] public int SubMeshIndex;
        [SerializeField] public char _Character = '*';

        private Material instanceMaterial;
        private ComputeBuffer positionBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        [NonSerialized] List<Vector4> _Positions;

        public char character
        {
            get => _Character;
            set
            {
                if (value != _Character)
                {
                    _Character = value;
                    UpdateBuffers();
                }
            }
        }

        private void Start()
        {
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            instanceMaterial = new Material(VoxelMaterial);

            UpdateBuffers();
        }

        private void OnDisable()
        {
            if (positionBuffer != null)
                positionBuffer.Release();
            positionBuffer = null;

            if (argsBuffer != null)
                argsBuffer.Release();
            argsBuffer = null;
        }

        void UpdateBuffers()
        {
            // Ensure submesh index is in range
            if (VoxelMesh != null)
                SubMeshIndex = Mathf.Clamp(SubMeshIndex, 0, VoxelMesh.subMeshCount - 1);

            // Positions
            UpdatePositions();
            if (positionBuffer != null)
                positionBuffer.Release();

            positionBuffer = new ComputeBuffer(_Positions.Count, 16);
            positionBuffer.SetData(_Positions);
            instanceMaterial.SetBuffer("positionBuffer", positionBuffer);

            // Indirect args
            if (VoxelMesh != null)
            {
                args[0] = VoxelMesh.GetIndexCount(SubMeshIndex);
                args[1] = (uint)_Positions.Count;
                args[2] = VoxelMesh.GetIndexStart(SubMeshIndex);
                args[3] = VoxelMesh.GetBaseVertex(SubMeshIndex);
            }
            else
            {
                args[0] = args[1] = args[2] = args[3] = 0;
            }
            argsBuffer.SetData(args);
        }

        void UpdatePositions()
        {
            int charactersPerLine = FontTexture.width / FontPoints.x;
            int lines = FontTexture.height / FontPoints.y;
            int numCharacters = charactersPerLine * lines;
            int maxVoxels = FontPoints.x * FontPoints.y;

            if (_Positions == null)
                _Positions = new List<Vector4>(maxVoxels);
            else
            {
                _Positions.Clear();
                if (_Positions.Capacity < maxVoxels)
                    _Positions.Capacity = maxVoxels;
            }

            if (_Character >= numCharacters)
                return;

            Vector2Int indexOffset = new Vector2Int
            {
                x = (_Character % charactersPerLine) * FontPoints.x,
                y = (_Character / charactersPerLine) * FontPoints.y
            };

            Vector4 offset = new Vector4(-FontPoints.x * 0.5f, -FontPoints.y * 0.5f);
            for (int x = 0; x < FontPoints.x; ++x)
            {
                for (int y = 0; y < FontPoints.y; ++y)
                {
                    Color c = FontTexture.GetPixel(indexOffset.x + x, indexOffset.y + y);
                    if (c.grayscale >= 0.5f)
                    {
                        _Positions.Add(new Vector4(x * VoxelDistance.x, y * VoxelDistance.y, 0f, 1f) + offset);
                    }
                }
            }
        }

        void Update()
        {
            // Render
            Graphics.DrawMeshInstancedIndirect(VoxelMesh, SubMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(8f, 8f, 1f)), argsBuffer);
        }
    }
}
