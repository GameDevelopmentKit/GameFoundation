using System;
using System.Collections.Generic;
using System.Linq;

namespace Com.ForbiddenByte.OSA.Util
{
    /*
        License type: MIT
        Quoted from github: "A short and simple permissive license with conditions
        only requiring preservation of copyright and license notices. Licensed works, modifications,
        and larger works may be distributed under different terms and without source code"
    */

    /*
        Copyright (c) 2011, 2012, 2013, 2014, 2015, 2016 Jake Gordon and contributors
        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
    */

    /// <summary>
    /// Heavily modified version of https://github.com/cariquitanmac/2D-Bin-Pack-Binary-Search,
    /// which is a C# implementation of Jakes Gordon Binary Tree Algorithm for 2D Bin Packing https://github.com/jakesgordon/bin-packing/
    /// <para>All rights go to the original author.</para>
    /// </summary>
    public class Packer2DBox
    {
        private List<Box>            _Boxes;
        private Node                 _RootNode;
        private double               _Spacing;
        private bool                 _AlternatingStrategyBiggerRightNode;
        private bool                 _AlternatingOtherStrategyBiggerRightNode;
        private double               _ContainerWidth;
        private double               _ContainerHeight;
        private NodeChoosingStrategy _ChoosingStrategy;

        public Packer2DBox(double containerWidth, double containerHeight, double spacing)
        {
            this._ContainerWidth  = containerWidth;
            this._ContainerHeight = containerHeight;
            this._Spacing         = spacing;
        }

        public void Pack(List<Box> boxes, bool sort, NodeChoosingStrategy choosingStrategy, out double totalWidth, out double totalHeight)
        {
            this._ChoosingStrategy = choosingStrategy;

            this._AlternatingStrategyBiggerRightNode = this._ChoosingStrategy == NodeChoosingStrategy.ALTERNATING_START_WITH_RIGHT;

            this._RootNode = new(0d, 0d) { height = this._ContainerHeight, width = this._ContainerWidth };
            this._Boxes    = boxes;
            if (sort)
                // Biggest boxes first with maxside, then secondarily by volume 
                // More info: https://codeincomplete.com/posts/bin-packing/
                this._Boxes.Sort((a, b) =>
                {
                    var aMax = Math.Max(a.width, a.height);
                    var bMax = Math.Max(b.width, b.height);

                    if (aMax != bMax) return (int)(bMax - aMax);

                    return (int)(b.volume - a.volume);
                });
            //_Boxes = _Boxes.Sort((a, b) =>
            //{
            //	var aMax = Math.Max(a.width, a.height);
            //	var bMax = Math.Max(b.width, b.height);
            //	if (aMax != bMax)
            //		return (int)(bMax - aMax);
            //	return (int)(b.volume - a.volume);
            //});
            //_Boxes = _Boxes.OrderByDescending(x => Math.Max(x.width, x.height)).ToList();
            ////_Boxes = _Boxes.OrderByDescending(x => x.volume).ToList();
            totalWidth  = 0f;
            totalHeight = 0f;
            foreach (var box in this._Boxes)
            {
                Node node = null;
                this.FindNode(this._RootNode, box.width, box.height, ref node);

                if (node != null)
                {
                    // Split rectangles
                    box.position = this.SplitNode(node, box.width, box.height);

                    var width                          = box.position.x + box.width;
                    if (width > totalWidth) totalWidth = width;

                    var height                            = box.position.y + box.height;
                    if (height > totalHeight) totalHeight = height;
                }
            }
        }

        private void FindNode(Node rootNode, double boxWidth, double boxHeight, ref Node node)
        {
            if (rootNode.isOccupied)
            {
                this.FindNode(rootNode.rightNode, boxWidth, boxHeight, ref node);
                this.FindNode(rootNode.bottomNode, boxWidth, boxHeight, ref node);
            }
            else if (boxWidth <= rootNode.width && boxHeight <= rootNode.height)
            {
                if (node == null || rootNode.distFromOrigin < node.distFromOrigin) node = rootNode;
            }
        }

        private Node SplitNode(Node node, double boxWidth, double boxHeight)
        {
            node.isOccupied = true;

            var rightNodeFullWidth  = node.width - (boxWidth + this._Spacing);
            var rightNodeFullHeight = node.height;

            var bottomNodeFullWidth  = node.width;
            var bottomNodeFullHeight = node.height - (boxHeight + this._Spacing);

            bool biggerRightNode;

            var localStrategy = this._ChoosingStrategy;
            if (localStrategy == NodeChoosingStrategy.MAX_VOLUME)
            {
                var rightVolume  = rightNodeFullWidth * rightNodeFullHeight;
                var bottomVolume = bottomNodeFullWidth * bottomNodeFullHeight;

                if (rightVolume == bottomVolume)
                {
                    // In case of equality, alternate between what we chose the last time
                    biggerRightNode                               = this._AlternatingOtherStrategyBiggerRightNode;
                    this._AlternatingOtherStrategyBiggerRightNode = !this._AlternatingOtherStrategyBiggerRightNode;
                }
                else
                {
                    biggerRightNode = rightVolume > bottomVolume;
                }
            }
            else if (localStrategy == NodeChoosingStrategy.MAX_SIDE)
            {
                var rightMaxSide  = Math.Max(rightNodeFullWidth, rightNodeFullHeight);
                var bottomMaxSide = Math.Max(bottomNodeFullWidth, bottomNodeFullHeight);

                if (rightMaxSide == bottomMaxSide)
                {
                    // In case of equality, alternate between what we chose the last time
                    biggerRightNode                               = this._AlternatingOtherStrategyBiggerRightNode;
                    this._AlternatingOtherStrategyBiggerRightNode = !this._AlternatingOtherStrategyBiggerRightNode;
                }
                else
                {
                    biggerRightNode = rightMaxSide > bottomMaxSide;
                }
            }
            else if (localStrategy == NodeChoosingStrategy.MAX_SIDE)
            {
                var rightMaxSide  = Math.Max(rightNodeFullWidth, rightNodeFullHeight);
                var bottomMaxSide = Math.Max(bottomNodeFullWidth, bottomNodeFullHeight);

                if (rightMaxSide == bottomMaxSide)
                {
                    // In case of equality, alternate between what we chose the last time
                    biggerRightNode                               = this._AlternatingOtherStrategyBiggerRightNode;
                    this._AlternatingOtherStrategyBiggerRightNode = !this._AlternatingOtherStrategyBiggerRightNode;
                }
                else
                {
                    biggerRightNode = rightMaxSide > bottomMaxSide;
                }
            }
            else if (localStrategy == NodeChoosingStrategy.MAX_SUM)
            {
                var rightSum  = rightNodeFullWidth + rightNodeFullHeight;
                var bottomSum = bottomNodeFullWidth + bottomNodeFullHeight;

                if (rightSum == bottomSum)
                {
                    // In case of equality, alternate between what we chose the last time
                    biggerRightNode                               = this._AlternatingOtherStrategyBiggerRightNode;
                    this._AlternatingOtherStrategyBiggerRightNode = !this._AlternatingOtherStrategyBiggerRightNode;
                }
                else
                {
                    biggerRightNode = rightSum > bottomSum;
                }
            }
            else
            {
                if (this._ChoosingStrategy == NodeChoosingStrategy.RIGHT)
                {
                    biggerRightNode = true;
                }
                else if (this._ChoosingStrategy == NodeChoosingStrategy.BOTTOM)
                {
                    biggerRightNode = false;
                }
                else
                {
                    // Alternating
                    biggerRightNode                          = this._AlternatingStrategyBiggerRightNode;
                    this._AlternatingStrategyBiggerRightNode = !this._AlternatingStrategyBiggerRightNode;
                }
            }

            node.rightNode = new(node.x + (boxWidth + this._Spacing), node.y)
            {
                depth  = node.depth + 1,
                width  = rightNodeFullWidth,
                height = biggerRightNode ? node.height : boxHeight,
            };
            node.bottomNode = new(node.x, node.y + (boxHeight + this._Spacing))
            {
                depth  = node.depth + 1,
                width  = biggerRightNode ? boxWidth : node.width,
                height = bottomNodeFullHeight,
            };

            return node;
        }

        public class Node
        {
            public          int    depth;
            public          Node   rightNode;
            public          Node   bottomNode;
            public          double x;
            public          double y;
            public          double width;
            public          double height;
            public readonly double distFromOrigin;
            public          bool   isOccupied;

            public Node(double x, double y)
            {
                this.x              = x;
                this.y              = y;
                this.distFromOrigin = Math.Sqrt(x * x + y * y);
            }
        }

        public class Box
        {
            public double height;
            public double width;
            public double volume;
            public Node   position;

            public Box(double width, double height)
            {
                this.width  = width;
                this.height = height;
                this.volume = width * height;
            }
        }

        /// <summary>Note expanding choices, in order of success rate</summary>
        public enum NodeChoosingStrategy
        {
            MAX_VOLUME,
            MAX_SUM,
            MAX_SIDE,
            ALTERNATING_START_WITH_BOTTOM,
            RIGHT,
            BOTTOM,
            ALTERNATING_START_WITH_RIGHT, // same as BOTTOM, in 99% of cases
            COUNT_,
        }
    }
}