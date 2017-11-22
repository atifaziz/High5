#region Copyright (c) 2017 Atif Aziz
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

namespace High5.Tests
{
    using System.Linq;
    using Xunit;
    using static HtmlNodeFactory;

    public class HtmlTreeTests
    {
        static HtmlTree<HtmlNode> DefaultHtmlTree => default(HtmlTree<HtmlNode>);

        [Fact]
        public void DefaultNodeIsNull() =>
            Assert.Null(default(HtmlTree<HtmlNode>).Node);

        [Fact]
        public void DefaultIsEmpty() =>
            Assert.True(DefaultHtmlTree.IsEmpty);

        [Fact]
        public void DefaultHasChildNodesIsFalse() =>
            Assert.False(DefaultHtmlTree.HasChildNodes);

        [Fact]
        public void DefaultChildNodeCountIsZero() =>
            Assert.Equal(0, DefaultHtmlTree.ChildNodeCount);

        [Fact]
        public void DefaultChildNodesIsEmpty() =>
            Assert.Empty(DefaultHtmlTree.ChildNodes);

        [Fact]
        public void DefaultFirstChildIsNull() =>
            Assert.Null(DefaultHtmlTree.FirstChild);

        [Fact]
        public void DefaultLastChildIsNull() =>
            Assert.Null(DefaultHtmlTree.LastChild);

        [Fact]
        public void DefaultHasParentIsFalse() =>
            Assert.False(DefaultHtmlTree.HasParent);

        [Fact]
        public void DefaultParentIsNull() =>
            Assert.Null(DefaultHtmlTree.Parent);

        [Fact]
        public void DefaultHashCodeIsZero() =>
            Assert.Equal(0, DefaultHtmlTree.GetHashCode());

        [Fact]
        public void DefaultEqualsSelf()
        {
            Assert.True(DefaultHtmlTree.Equals(DefaultHtmlTree));
            Assert.True(DefaultHtmlTree.Equals((object) DefaultHtmlTree));
            Assert.True(DefaultHtmlTree == default(HtmlTree<HtmlNode>));
            Assert.False(DefaultHtmlTree != default(HtmlTree<HtmlNode>));
        }

        [Fact]
        public void DefaultNotEqualsNull() =>
            Assert.False(DefaultHtmlTree.Equals(null));

        [Fact]
        public void DefaultNotEqualsOther() =>
            Assert.False(DefaultHtmlTree.Equals(new object()));

        [Fact]
        public void DefaultDescendantsIsEmpty() =>
            Assert.Empty(DefaultHtmlTree.Descendants());

        [Fact]
        public void AllRelationsAreReflectedCorrectlyThroughTree()
        {
            HtmlElement html, head, body, p;
            var doc =
                Document(
                    html = Element("html",
                        head = Element("head",
                            Element("title", Text("Example"))),
                        body = Element("body",
                            Element("h1", Attributes(("class", "main")),
                                Text("Heading")),
                            p = Element("p",
                                Text("Lorem ipsum dolor sit amet, consectetur adipiscing elit.")))));

            var tree = HtmlTree.Create(doc);
            Assert.Same(doc, tree.Node);
            Assert.False(tree.HasParent);
            Assert.True(tree.HasChildNodes);
            Assert.Equal(1, tree.ChildNodeCount);
            Assert.True(tree.FirstChild.Equals(tree.LastChild));

            var treeHtml = tree.ChildNodes.First();
            Assert.Same(html, treeHtml.Node);
            Assert.True(tree.Equals(treeHtml.Parent));
            Assert.Equal(2, treeHtml.ChildNodeCount);
            Assert.False(treeHtml.FirstChild.Equals(treeHtml.LastChild));

            var treeHead = treeHtml.ChildNodes.First();
            Assert.True(treeHead.Equals(treeHtml.FirstChild));
            Assert.Same(head, treeHead.Node);
            Assert.True(treeHead.Parent == treeHtml);
            Assert.Equal(1, treeHead.ChildNodeCount);

            var treeBody = treeHtml.ChildNodes.Last();
            Assert.True(treeBody.Equals(treeHtml.LastChild));
            Assert.Same(body, treeBody.Node);
            Assert.True(treeBody.Parent == treeHtml);
            Assert.Equal(2, treeBody.ChildNodeCount);

            Assert.Equal(doc.Descendants(),
                         from d in tree.Descendants()
                         select d.Node);

            Assert.Equal(doc.DescendantsAndSelf(),
                         from d in tree.DescendantsAndSelf()
                         select d.Node);

            var treePara = HtmlTree.Create(p);
            Assert.True(treePara.Equals(treePara.ChildNodes.Single().Parent));
        }
    }
}
