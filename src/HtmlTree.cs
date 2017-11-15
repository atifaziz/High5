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

    public interface IHtmlTree
    {
        IHtmlTreeNode Root { get; }
        IHtmlTreeNode GetParent(HtmlNode node);
    }

    public interface IHtmlTreeRootable { }

    public interface IHtmlTree<T> : IHtmlTree
        where T : HtmlNode, IHtmlTreeRootable
    {
        new HtmlTreeNode<T, T> Root { get; }
        bool TryGetParent(HtmlNode node, out HtmlTreeNode<T, HtmlNode> parent);
    }

    public interface IHtmlTreeNode
    {
        IHtmlTree Tree { get; }
        HtmlNode Node { get; }
    }

    public interface IHtmlTreeNode<TRoot, out TNode> : IHtmlTreeNode
        where TRoot : HtmlNode, IHtmlTreeRootable
        where TNode : HtmlNode
    {
        new IHtmlTree<TRoot> Tree { get; }
        new TNode Node { get; }
    }

    public static class HtmlTreeNode
    {
        public static HtmlTreeNode<TRoot, TNode> Create<TRoot, TNode>(IHtmlTree<TRoot> root, TNode node)
            where TRoot : HtmlNode, IHtmlTreeRootable
            where TNode : HtmlNode =>
            new HtmlTreeNode<TRoot, TNode>(root, node);
    }

    public struct HtmlTreeNode<TRoot, TNode> :
        IHtmlTreeNode<TRoot, TNode>,
        IEquatable<HtmlTreeNode<TRoot, TNode>>
        where TRoot : HtmlNode, IHtmlTreeRootable
        where TNode : HtmlNode
    {
        public HtmlTreeNode(IHtmlTree<TRoot> tree, TNode node)
        {
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public IHtmlTree<TRoot> Tree { get; }
        public TNode Node { get; }

        public bool HasValue => Tree == null;

        public HtmlTreeNode<TRoot, HtmlNode> AsBaseNode() => Tree.WithNode((HtmlNode) Node);

        public int ChildNodeCount => Node.ChildNodes.Count;
        public bool HasChildNode  => ChildNodeCount > 0;

        public IEnumerable<HtmlTreeNode<TRoot, HtmlNode>> ChildNodes
        {
            get
            {
                var tree = Tree;
                return tree != null
                     ? from child in Node.ChildNodes
                       select tree.WithNode(child)
                     : Enumerable.Empty<HtmlTreeNode<TRoot, HtmlNode>>();
            }
        }

        public HtmlTreeNode<TRoot, HtmlNode> Parent =>
            Tree.TryGetParent(Node, out var parent)
            ? parent
            : default(HtmlTreeNode<TRoot, HtmlNode>);

        public HtmlTreeNode<TRoot, HtmlNode> PreviousSibling => GetSibling(-1, (i, _) => i >= 0);
        public HtmlTreeNode<TRoot, HtmlNode> NextSibling => GetSibling(+1, (i, count) => i < count);

        HtmlTreeNode<TRoot, HtmlNode> GetSibling(int offset, Func<int, int, bool> predicate)
        {
            var siblings = Parent.Node.ChildNodes;
            var index = -1;
            for (var i = 0; i < siblings.Count; i++)
            {
                var sibling = siblings[i];
                if (sibling == Node)
                {
                    index = i;
                    break;
                }
            }
            return index >= 0  && predicate(index + offset, siblings.Count)
                 ? Tree.WithNode(siblings[index + offset])
                 : default(HtmlTreeNode<TRoot, HtmlNode>);
        }

        public IEnumerable<HtmlTreeNode<TRoot, HtmlNode>> NodesBeforeSelf()
        {
            var node = AsBaseNode();
            while ((node = node.PreviousSibling).HasValue)
                yield return node;
        }

        public IEnumerable<HtmlTreeNode<TRoot, HtmlNode>> NodesAfterSelf()
        {
            var node = Tree.WithNode((HtmlNode) Node);
            while ((node = node.NextSibling).HasValue)
                yield return node;
        }

        public IEnumerable<HtmlTreeNode<TRoot, HtmlNode>> Descendants()
        {
            foreach (var child in ChildNodes)
            {
                yield return child;
                foreach (var descendant in child.Descendants())
                    yield return descendant;
            }
        }

        public IEnumerable<HtmlTreeNode<TRoot, HtmlNode>> DescendantsAndSelf() =>
            Enumerable.Repeat(AsBaseNode(), 1).Concat(Descendants());

        public IEnumerable<HtmlTreeNode<TRoot, HtmlNode>> Ancestors()
        {
            for (var node = Parent; node.HasValue; node = node.Parent)
                yield return node;
        }

        public IEnumerable<HtmlTreeNode<TRoot, HtmlNode>> AncestorsAndSelf() =>
            Enumerable.Repeat(AsBaseNode(), 1).Concat(Ancestors());

        IHtmlTree IHtmlTreeNode.Tree => Tree;
        HtmlNode  IHtmlTreeNode.Node => Node;

        public bool Equals(HtmlTreeNode<TRoot, TNode> other) =>
            Tree == other.Tree && Node == other.Node;

        public override bool Equals(object obj) =>
            obj is HtmlTreeNode<TRoot, TNode> node && Equals(node);

        public override int GetHashCode() =>
            unchecked((Tree?.GetHashCode() ?? 0) * 397) ^ (Node?.GetHashCode() ?? 0);

        public static bool operator ==(HtmlTreeNode<TRoot, TNode> left, HtmlTreeNode<TRoot, TNode> right) =>
            left.Equals(right);

        public static bool operator !=(HtmlTreeNode<TRoot, TNode> left, HtmlTreeNode<TRoot, TNode> right) =>
            !left.Equals(right);
    }

    public static class HtmlTree
    {
        public static IHtmlTree<HtmlDocument> Create(HtmlDocument document) =>
            Tree<HtmlDocument>.Create(document);

        public static IHtmlTree<HtmlDocumentFragment> Create(HtmlDocumentFragment documentFragment) =>
            Tree<HtmlDocumentFragment>.Create(documentFragment);

        public static IHtmlTree<HtmlElement> Create(HtmlElement element) =>
            Tree<HtmlElement>.Create(element);

        sealed class Tree<T> : IHtmlTree<T> where T : HtmlNode, IHtmlTreeRootable
        {
            readonly Dictionary<HtmlNode, HtmlNode> _parentByNode;

            Tree(T root, Dictionary<HtmlNode, HtmlNode> parentByNode)
            {
                Root = this.WithNode(root);
                _parentByNode = parentByNode;
            }

            public HtmlTreeNode<T, T> Root { get; }

            IHtmlTreeNode IHtmlTree.Root => Root;

            IHtmlTreeNode IHtmlTree.GetParent(HtmlNode node) =>
                TryGetParent(node, out var parent) ? (IHtmlTreeNode) parent : null;

            public bool TryGetParent(HtmlNode node, out HtmlTreeNode<T, HtmlNode> parent)
            {
                if (node == null) throw new ArgumentNullException(nameof(node));

                if (node == Root.Node)
                {
                    parent = default(HtmlTreeNode<T, HtmlNode>);
                    return false;
                }

                if (!_parentByNode.TryGetValue(node, out var parentNode))
                    throw new ArgumentException("Node does not belong to this tree.");

                parent = this.WithNode(parentNode);
                return true;
                /*
                return node != Root.Node
                     ? _parentByNode.TryGetValue(node, out var parentNode)
                       ? new HtmlTreeNode<T, HtmlNode>(this, parentNode)
                       : throw new ArgumentException("Node does not belong to this tree.")
                     : default(HtmlTreeNode<T, HtmlNode>);
                     */
            }

            internal static Tree<T> Create(T root)
            {
                var parentByNode = new Dictionary<HtmlNode, HtmlNode>();
                Walk(root);

                void Walk(HtmlNode parent)
                {
                    foreach (var child in parent.ChildNodes)
                    {
                        parentByNode.Add(child, parent);
                        Walk(child);
                    }
                }

                return new Tree<T>(root, parentByNode);
            }
        }
    }

    public static class HtmlTreeExtensions
    {
        public static HtmlTreeNode<TRoot, TNode> WithNode<TRoot, TNode>(this IHtmlTree<TRoot> tree, TNode node)
            where TRoot : HtmlNode, IHtmlTreeRootable
            where TNode : HtmlNode =>
            HtmlTreeNode.Create(tree, node);

        public static IEnumerable<HtmlTreeNode<TRoot, HtmlElement>> Elements<TRoot, TNode>(this IEnumerable<HtmlTreeNode<TRoot, TNode>> nodes)
            where TRoot : HtmlNode, IHtmlTreeRootable
            where TNode : HtmlNode =>
            from node in nodes.Where(e => e.Node is HtmlElement) select node.Tree.WithNode((HtmlElement) (object) node.Node);
        /*
        public static IEnumerable<HtmlElement> Elements(this HtmlNode node) =>
            node?.ChildNodes.Elements()
            ?? throw new ArgumentNullException(nameof(node));

        public static IEnumerable<HtmlElement> ElementsAfterSelf(this HtmlNode node) =>
            node?.NodesAfterSelf().Elements()
            ?? throw new ArgumentNullException(nameof(node));

        public static IEnumerable<HtmlElement> ElementsBeforeSelf(this HtmlNode node) =>
            node?.NodesBeforeSelf().Elements()
            ?? throw new ArgumentNullException(nameof(node));
            */
        /*
        public static HtmlNode GetPreviousSibling(this IHtmlTree tree, HtmlNode node) =>
            GetSibling(tree, node, -1, (i, _) => i >= 0);
        public static HtmlNode GetNextSibling(this IHtmlTree tree, HtmlNode node) =>
            GetSibling(tree, node, +1, (i, count) => i < count);

        public static HtmlNode GetSibling(this IHtmlTree tree, HtmlNode node, int offset, Func<int, int, bool> predicate)
        {
            if (tree == null) throw new ArgumentNullException(nameof(tree));
            if (node == null) throw new ArgumentNullException(nameof(node));

            var siblings = tree.GetParent(node)?.ChildNodes;
            var index = siblings?.IndexOf(node);
            return index is int i && predicate(i + offset, siblings.Count)
                ? siblings[i + offset]
                : null;
        }

        public static IEnumerable<HtmlNode> NodesBeforeSelf(this IHtmlTree tree, HtmlNode node)
        {
            while ((node = tree.GetPreviousSibling(node)) != null)
                yield return node;
        }

        public static IEnumerable<HtmlNode> NodesAfterSelf(this IHtmlTree tree, HtmlNode node)
        {
            while ((node = tree.GetNextSibling(node)) != null)
                yield return node;
        }

        public static IEnumerable<HtmlNode> Ancestors(this IHtmlTree tree, HtmlNode node)
        {
            for (var parent = tree.GetParent(node); parent != null; parent = parent.ParentNode)
                yield return parent;
        }

        public static IEnumerable<HtmlNode> AncestorsAndSelf(this IHtmlTree tree, HtmlNode node) =>
            Enumerable.Repeat(node, 1).Concat(tree.Ancestors(node));
        */
    }
}
