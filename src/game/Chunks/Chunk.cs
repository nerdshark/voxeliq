﻿/*
 * Copyright (C) 2011-2012 voxeliq project 
 *
 */

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VolumetricStudios.VoxeliqGame.Blocks;
using VolumetricStudios.VoxeliqGame.Debugging;
using VolumetricStudios.VoxeliqGame.Graphics;
using VolumetricStudios.VoxeliqGame.Universe;
using VolumetricStudios.VoxeliqGame.Utils.Vector;

namespace VolumetricStudios.VoxeliqGame.Chunks
{
    /// <summary>
    /// Unit drawable chunk of blocks.
    /// </summary>
    public sealed class Chunk : IInGameDebuggable
    {
        public Chunk North { get { return ChunkStorage.Instance[this.RelativePosition.X, this.RelativePosition.Z + 1]; } }
        public Chunk South { get { return ChunkStorage.Instance[this.RelativePosition.X, this.RelativePosition.Z - 1]; } }
        public Chunk West { get { return ChunkStorage.Instance[this.RelativePosition.X - 1, this.RelativePosition.Z]; } }
        public Chunk East { get { return ChunkStorage.Instance[this.RelativePosition.X + 1, this.RelativePosition.Z]; } }
        public Chunk NorthWest { get { return ChunkStorage.Instance[this.RelativePosition.X - 1, this.RelativePosition.Z + 1]; } }
        public Chunk NorthEast { get { return ChunkStorage.Instance[this.RelativePosition.X + 1, this.RelativePosition.Z + 1]; } }
        public Chunk SouthWest { get { return ChunkStorage.Instance[this.RelativePosition.X - 1, this.RelativePosition.Z - 1]; } }
        public Chunk SouthEast { get { return ChunkStorage.Instance[this.RelativePosition.X + 1, this.RelativePosition.Z - 1]; } }

        /// <summary>
        /// Maximum sun value.
        /// </summary>
        public static byte MaxSunValue = 16;

        /// <summary>
        /// Chunk width in blocks.
        /// </summary>
        public static byte WidthInBlocks = 16;
        public static byte MaxWidthInBlocks = 15;

        /// <summary>
        /// Chunk lenght in blocks
        /// </summary>
        public static byte LenghtInBlocks = 16;
        public static byte MaxLenghtInBlocks = 15;

        /// <summary>
        /// Chunk height in blocks.
        /// </summary>
        public static byte HeightInBlocks = 128;
        public static byte MaxHeightInBlocks = 127;

        /// <summary>
        /// Chunk volume in blocks.
        /// </summary>
        public static readonly int Volume = WidthInBlocks * HeightInBlocks * LenghtInBlocks;

        /// <summary>
        /// The chunks world position.
        /// </summary>
        public Vector2Int WorldPosition { get; private set; }
        
        /// <summary>
        /// The chunks relative position.
        /// </summary>
        public Vector2Int RelativePosition { get; private set; }

        /// <summary>
        /// The bounding box for the chunk.
        /// </summary>
        public BoundingBox BoundingBox { get; set; }

        /// <summary>
        /// The chunk state.
        /// </summary>
        public ChunkState ChunkState { get; set; }

        public short Index;

        /// <summary>
        /// Highest solid blocks offset.
        /// </summary>
        public byte HighestSolidBlockOffset;

        /// <summary>
        /// Lowest empty block offset.
        /// </summary>
        public byte LowestEmptyBlockOffset =  (byte)HeightInBlocks;

        /// <summary>
        /// Is the region disposed already?
        /// </summary>
        public bool Disposed = false;

        /// <summary>
        /// Vertex buffer for chunk's blocks.
        /// </summary>
        public VertexBuffer VertexBuffer { get; set; }

        /// <summary>
        /// Index buffer for chunk's blocks.
        /// </summary>
        public IndexBuffer IndexBuffer { get; set; }

        /// <summary>
        /// The vertex list.
        /// </summary>
        public List<BlockVertex> VertexList;

        /// <summary>
        /// The index list.
        /// </summary>
        public List<short> IndexList;

        public Chunk(Vector2Int relativePosition)
        {
            this.ChunkState = ChunkState.AwaitingGenerate; // set initial state to awaiting generation.

            this.RelativePosition = relativePosition;
            this.WorldPosition = new Vector2Int(this.RelativePosition.X * WidthInBlocks, this.RelativePosition.Z * LenghtInBlocks);
            this.BoundingBox = new BoundingBox(new Vector3(WorldPosition.X, 0, WorldPosition.Z), new Vector3(this.WorldPosition.X + WidthInBlocks, HeightInBlocks, this.WorldPosition.Z + LenghtInBlocks));

            this.VertexList = new List<BlockVertex>();
            this.IndexList = new List<short>();
        }

        public bool IsInBounds(float x, float z)
        {
            if (x < this.BoundingBox.Min.X || z < this.BoundingBox.Min.Z || x >= this.BoundingBox.Max.X || z >= this.BoundingBox.Max.Z) return false;
            return true;
        }

        public Block BlockAt(int x, int y, int z)
        {
            return BlockStorage.GetByWorldPosition(this.WorldPosition.X + x, y, this.WorldPosition.Z + z);
        }

        public void SetBlock(byte x, byte y, byte z, Block block)
        {
            switch (block.Exists)
            {
                case false:
                    if (this.LowestEmptyBlockOffset > y && y > 0)
                        this.LowestEmptyBlockOffset = (byte) (y - 1);
                    break;
                case true:
                    if (y > this.HighestSolidBlockOffset && y < MaxHeightInBlocks)
                        this.HighestSolidBlockOffset = (byte) (y + 1);
                    break;
            }

            BlockStorage.SetByWorldPosition(this.WorldPosition.X + x, y, this.WorldPosition.Z + z, block);
            this.ChunkState = ChunkState.AwaitingRelighting;
        }

        public void PrintDebugInfo(GraphicsDevice graphicsDevice, ICamera camera, SpriteBatch spriteBatch, SpriteFont spriteFont)
        {
            var position = RelativePosition + " " + this.ChunkState;
            var positionSize = spriteFont.MeasureString(position);

            var projected = graphicsDevice.Viewport.Project(Vector3.Zero, camera.Projection, camera.View,
                                                            Matrix.CreateTranslation( new Vector3(WorldPosition.X + WidthInBlocks/2, HighestSolidBlockOffset - 1, WorldPosition.Z + LenghtInBlocks/2)));

            spriteBatch.DrawString(spriteFont, position, new Vector2(projected.X - positionSize.X/2, projected.Y - positionSize.Y/2), Color.Yellow);

            BoundingBoxRenderer.Render(this.BoundingBox , graphicsDevice, camera.View,camera.Projection, Color.DarkRed);
        }

        public override string ToString()
        {
            return RelativePosition.ToString();
        }

        #region de-ctor

        // IDisposable pattern: http://msdn.microsoft.com/en-us/library/fs2xkftw(v=VS.100).aspx

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Take object out the finalization queue to prevent finalization code for it from executing a second time.
        }

        private void Dispose(bool disposing)
        {
            if (this.Disposed) return; // if already disposed, just return

            if (disposing) // only dispose managed resources if we're called from directly or in-directly from user code.
            {
                this.IndexList.Clear();
                this.IndexList = null;
                this.VertexList.Clear();
                this.VertexList = null;

                if (this.VertexBuffer != null) 
                    this.VertexBuffer.Dispose();

                if (this.IndexBuffer != null) 
                    this.IndexBuffer.Dispose();
            }

            Disposed = true;
        }

        ~Chunk() { Dispose(false); } // finalizer called by the runtime. we should only dispose unmanaged objects and should NOT reference managed ones.    

        #endregion
    }
}
