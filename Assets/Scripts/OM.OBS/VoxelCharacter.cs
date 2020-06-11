using System;
using System.Collections.Generic;
using UnityEngine;

namespace OM.OBS
{
    public class VoxelCharacter : MonoBehaviour
    {
        public enum EffectKind
        {
            None,
            Forward,
            Backward
        };

        [SerializeField] public Mesh VoxelMesh;
        [SerializeField] public Vector2 VoxelDistance;
        [SerializeField] public Material VoxelMaterial;
        [SerializeField] public int SubMeshIndex;

        [Header("Mesh Instanced Properties")]
        [SerializeField, Range(0, 1)] public float Time;
        [SerializeField, Range(0, 1)] public float InstancedRotateDiff;
        [SerializeField, Range(0, 1)] public float InstancedScaleDiff;
        [SerializeField] public EffectKind RotateEffect;
        [SerializeField] public EffectKind ScaleEffect;

        private ComputeBuffer positionBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        private MaterialPropertyBlock propertyBlock;

        [NonSerialized] public Vector3 RotateAxis;
        [NonSerialized] public char Character;
        [NonSerialized] public List<Vector4> Positions;

        public bool IsValid()
        {
            return Positions == null;
        }

        public void Assign(char c, List<Vector4> posList)
        {
            Character = c;
            Positions = posList;
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

            propertyBlock.Clear();
            propertyBlock = null;
        }

        void UpdateBuffers()
        {
            // Ensure submesh index is in range
            if (VoxelMesh != null)
                SubMeshIndex = Mathf.Clamp(SubMeshIndex, 0, VoxelMesh.subMeshCount - 1);

            // Positions
            if (positionBuffer != null)
                positionBuffer.Release();

            positionBuffer = new ComputeBuffer(Positions.Count, 16);
            positionBuffer.SetData(Positions);

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            propertyBlock.SetBuffer("PositionBuffer", positionBuffer);

            // Indirect args
            if (VoxelMesh != null)
            {
                args[0] = VoxelMesh.GetIndexCount(SubMeshIndex);
                args[1] = (uint)Positions.Count;
                args[2] = VoxelMesh.GetIndexStart(SubMeshIndex);
                args[3] = VoxelMesh.GetBaseVertex(SubMeshIndex);
            }
            else
            {
                args[0] = args[1] = args[2] = args[3] = 0;
            }

            if (argsBuffer == null)
                argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            argsBuffer.SetData(args);
        }


        public void Render()
        {
            if (propertyBlock != null)
            {
                var instanceCount = (float)args[1];
                float rad, radDiff;
                float scale, scaleDiff;
                Vector3 rotateAxis = transform.worldToLocalMatrix.MultiplyPoint(RotateAxis).normalized;

                if (RotateEffect != EffectKind.None)
                {
                    radDiff = InstancedRotateDiff * 360f * Mathf.Deg2Rad;
                    if (RotateEffect == EffectKind.Backward)
                        radDiff = -radDiff;

                    var extra = (instanceCount - 1) * radDiff;
                    rad = Mathf.Lerp(
                        Mathf.Min(0, -extra),
                        Mathf.PI * 2 + Mathf.Max(0, -extra),
                        Time
                    );
                }
                else
                {
                    radDiff = 0;
                    rad = 0;
                }

                if (ScaleEffect != EffectKind.None)
                {
                    scaleDiff = InstancedScaleDiff;
                    if (ScaleEffect == EffectKind.Backward)
                        scaleDiff = -scaleDiff;

                    var extra = (instanceCount - 1) * scaleDiff;
                    scale = Mathf.Lerp(
                        Mathf.Min(0, -extra),
                        1 + Mathf.Max(0, -extra),
                        Time
                    );
                }
                else
                {
                    scaleDiff = 0;
                    scale = 1;
                }

                propertyBlock.SetMatrix("ObjectToWorld", transform.localToWorldMatrix);
                propertyBlock.SetVector("AxisAngle", new Vector4(rotateAxis.x, rotateAxis.y, rotateAxis.z, rad));
                propertyBlock.SetVector("InstancedArgs", new Vector4(radDiff, scaleDiff, 0, scale));

                // Render
                Graphics.DrawMeshInstancedIndirect(
                    mesh: VoxelMesh,
                    submeshIndex: SubMeshIndex,
                    material: VoxelMaterial,
                    properties: propertyBlock,
                    bounds: new Bounds(Vector3.zero, new Vector3(12f, 12f, 12f)),
                    bufferWithArgs: argsBuffer,
                    argsOffset: 0);
            }
        }
    }
}
