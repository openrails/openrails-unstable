﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using Orts.Formats.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Support for animating any sub-part of a wagon or locomotive. Supports both on/off toggled animations and continuous-running ones.
    /// </summary>
    public class AnimatedPart
    {
        // Shape that we're animating.
        readonly PoseableShape PoseableShape;

        /// <summary>
        /// .S file: number of animation key-frames that are used by this part. This is calculated from the matrices provided.
        /// glTF file: the frames are measured in seconds, so the frame count is actually the total length of the animation clip in seconds.
        /// </summary>
        public float FrameCount;

        /// <summary>
        /// .S file: Current frame of the animation.
        /// glTF file: The actual time in seconds within the animation clip.
        /// </summary>
        float AnimationKey;

        /// <summary>
        /// .S file: List of the matrices we're animating for this part.
        /// glTF file: The animation clip's numbers we are playing for this part.
        /// </summary>
        public List<int> MatrixIndexes = new List<int>();

        /// <summary>
        /// Construct with a link to the shape that contains the animated parts 
        /// </summary>
        public AnimatedPart(PoseableShape poseableShape)
        {
            PoseableShape = poseableShape;
        }

        /// <summary>
        /// All the matrices associated with this part are added during initialization by the MSTSWagon constructor
        /// </summary>
        public void AddMatrix(int matrix)
        {
            if (matrix < 0) return;
            MatrixIndexes.Add(matrix);
            UpdateFrameCount(matrix);
        }

        void UpdateFrameCount(int matrix)
        {
            // .S file:
            if (PoseableShape.SharedShape.Animations != null
                && PoseableShape.SharedShape.Animations.Count > 0
                && PoseableShape.SharedShape.Animations[0].anim_nodes.Count > matrix
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].Count > 0)
            {
                FrameCount = Math.Max(FrameCount, PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].ToArray().Cast<KeyPosition>().Last().Frame);
                // Sometimes there are more frames in the second controller than in the first
                if (PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 1
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[1].Count > 0)
                    FrameCount = Math.Max(FrameCount, PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[1].ToArray().Cast<KeyPosition>().Last().Frame);
            }

            // glTF file:
            if (PoseableShape.SharedShape is GltfShape gltfShape && gltfShape.HasAnimation(matrix))
            {
                // Use the clip's length in time as the frame count
                FrameCount = gltfShape.GetAnimationLength(matrix);
            }
            else
            {
                for (var i = 0; i < PoseableShape.Hierarchy.Length; i++)
                    if (PoseableShape.Hierarchy[i] == matrix)
                        UpdateFrameCount(i);
            }
        }

        /// <summary>
        /// Ensure the shape file contained parts of this type 
        /// and those parts have an animation section.
        /// </summary>
        public bool Empty()
        {
            return MatrixIndexes.Count == 0;
        }

        void SetFrame(float frame)
        {
            AnimationKey = frame;
            foreach (var matrix in MatrixIndexes)
                PoseableShape.AnimateMatrix(matrix, AnimationKey);
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst clamping it to the frame count range.
        /// </summary>
        public void SetFrameClamp(float frame)
        {
            if (frame > FrameCount) frame = FrameCount;
            if (frame < 0) frame = 0;
            SetFrame(frame);
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst cycling back to the start as input goes beyond the last frame.
        /// </summary>
        public void SetFrameCycle(float frame)
        {
            // Animates from 0-FrameCount then FrameCount-0 for values of 0>=frame<=2*FrameCount.
            SetFrameClamp(FrameCount - Math.Abs(frame - FrameCount));
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst wrapping it around the frame count range.
        /// </summary>
        public void SetFrameWrap(float frame)
        {
            // Wrap the frame around 0-FrameCount without hanging when FrameCount=0.
            while (FrameCount > 0 && frame < 0) frame += FrameCount;
            if (frame < 0) frame = 0;
            frame %= FrameCount;
            SetFrame(frame);
        }

        /// <summary>
        /// Bypass the normal slow transition and jump the part immediately to this new state
        /// </summary>
        public void SetState(bool state)
        {
            SetFrame(state ? FrameCount : 0);
        }

        /// <summary>
        /// Updates an animated part that toggles between two states (e.g. pantograph, doors, mirrors).
        /// </summary>
        public void UpdateState(bool state, ElapsedTime elapsedTime)
        {
            SetFrameClamp(AnimationKey + (state ? 1 : -1) * elapsedTime.ClockSeconds);
        }

        /// <summary>
        /// Updates an animated part that toggles between two states and returns relative value of 
        /// animation key (between 0 and 1).
        /// </summary>
        public float UpdateAndReturnState(bool state, ElapsedTime elapsedTime)
        {
            SetFrameClamp(AnimationKey + (state ? 1 : -1) * elapsedTime.ClockSeconds);
            return AnimationKey / FrameCount;
        }

        /// <summary>
        /// Returns the animation key fraction (between 0 and 1)
        /// </summary>
        public float AnimationKeyFraction()
        {
            return AnimationKey / FrameCount;
        }

        /// <summary>
        /// Updates an animated part that loops (e.g. running gear), changing by the given amount.
        /// </summary>
        public void UpdateLoop(float change)
        {
            if (PoseableShape.SharedShape.Animations?.Count > 0 && FrameCount > 0)
            {
                // .S shape
                // The speed of rotation is set at 8 frames of animation per rotation at 30 FPS (so 16 frames = 60 FPS, etc.).
                var frameRate = PoseableShape.SharedShape.Animations[0].FrameRate * 8 / 30f;
                SetFrameWrap(AnimationKey + change * frameRate);
            }
            else if (PoseableShape.SharedShape is GltfShape gltfShape && gltfShape.GetAnimationLength(MatrixIndexes.FirstOrDefault()) > 0)
            {
                // glTf shape
                // Assume the animation is 1 loop long
                var loopLength = FrameCount;
                SetFrameWrap(AnimationKey + change * loopLength);
            }
        }

        /// <summary>
        /// Updates an animated part that loops only when enabled (e.g. wipers).
        /// </summary>
        public void UpdateLoop(bool running, ElapsedTime elapsedTime, float frameRateMultiplier = 1.5f)
        {
            if (PoseableShape.SharedShape.Animations?.Count > 0 && FrameCount > 0)
            {
                // .S shape
                // The speed of cycling is as default 1.5 frames of animation per second at 30 FPS.
                var frameRate = PoseableShape.SharedShape.Animations[0].FrameRate * frameRateMultiplier / 30f;
                if (running || (AnimationKey > 0 && AnimationKey + elapsedTime.ClockSeconds * frameRate < FrameCount))
                    SetFrameWrap(AnimationKey + elapsedTime.ClockSeconds * frameRate);
                else
                    SetFrame(0);
            }
            else if (PoseableShape.SharedShape is GltfShape gltfShape && gltfShape.GetAnimationLength(MatrixIndexes.FirstOrDefault()) > 0)
            {
                // glTf shape
                if (running || (AnimationKey > 0 && AnimationKey + elapsedTime.ClockSeconds < FrameCount))
                    SetFrameWrap(AnimationKey + elapsedTime.ClockSeconds * frameRateMultiplier);
                // In glTF multiple animations may target the same node, so we must not SetFrame(0) in the update.
            }
        }

        /// <summary>
        /// Swap the pointers around.
        /// </summary>
        public static void Swap(ref AnimatedPart a, ref AnimatedPart b)
        {
            AnimatedPart temp = a;
            a = b;
            b = temp;
        }
    }
}
