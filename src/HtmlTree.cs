#region Copyright (c) 2017 Atif Aziz, Adrian Guerra
//
// Portions Copyright (c) 2013 Ivan Nikulin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#endregion

namespace High5
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Internal;
    using Collections;

    public struct HtmlTree<TNode> : IEquatable<HtmlTree<TNode>>
        where TNode : HtmlNode
    {
        readonly ListNode<HtmlNode> _ancestors;

        internal HtmlTree(TNode node, ListNode<HtmlNode> ancestors)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            _ancestors = ancestors ?? throw new ArgumentNullException(nameof(ancestors));
        }

        public bool HasParent => _ancestors?.IsEmpty == false;

        public HtmlTree<HtmlNode> Parent =>
            HasValue
            ? HasParent
              ? HtmlTree.Create(_ancestors.Item, _ancestors.Next)
              : default(HtmlTree<HtmlNode>)
            : throw new InvalidOperationException();

        public TNode Node { get; }

        public bool HasValue => Node != null;

        public HtmlTree<HtmlNode> AsBaseNode() => HtmlTree.Create((HtmlNode) Node, _ancestors);

        public int ChildNodeCount => Node.ChildNodes.Count;
        public bool HasChildNodes => ChildNodeCount > 0;

        public IEnumerable<HtmlTree<HtmlNode>> ChildNodes
        {
            get
            {
                var parent = Node;
                var ancestors = _ancestors.Prepend(parent);
                return from child in parent.ChildNodes
                       select HtmlTree.Create(child, ancestors);
            }
        }

        public bool Equals(HtmlTree<TNode> other) =>
            (ReferenceEquals(_ancestors, other._ancestors) || _ancestors == other._ancestors)
            && Node == other.Node;

        public override bool Equals(object obj) =>
            obj is HtmlTree<TNode> node && Equals(node);

        public override int GetHashCode()
        {
            var hash = HashCodeCombiner.Start();
            hash.Add(_ancestors?.GetHashCode());
            hash.Add(Node);
            return hash.CombinedHash;
        }

        public static bool operator ==(HtmlTree<TNode> left, HtmlTree<TNode> right) =>
            left.Equals(right);

        public static bool operator !=(HtmlTree<TNode> left, HtmlTree<TNode> right) =>
            !left.Equals(right);
    }

    public static class HtmlTree
    {
        internal static HtmlTree<TNode> Create<TNode>(TNode node, ListNode<HtmlNode> ancestors)
            where TNode : HtmlNode =>
            new HtmlTree<TNode>(node, ancestors);

        public static HtmlTree<HtmlDocument> Create(HtmlDocument document) =>
            TreeFromNode(document);

        public static HtmlTree<HtmlDocumentFragment> Create(HtmlDocumentFragment documentFragment) =>
            TreeFromNode(documentFragment);

        public static HtmlTree<HtmlElement> Create(HtmlElement element) =>
            TreeFromNode(element);

        static HtmlTree<T> TreeFromNode<T>(T root) where T : HtmlNode =>
            Create(root, ListNode<HtmlNode>.Empty);
    }
}
