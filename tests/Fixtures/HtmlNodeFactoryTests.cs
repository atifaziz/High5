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

    public class HtmlNodeFactoryTests
    {
        const string HtmlNs = "http://www.w3.org/1999/xhtml";

        public class Text
        {
            [Fact]
            public void ReturnsTextNodeInitialzed()
            {
                const string value = "foobar";
                var text = Text(value);
                Assert.NotNull(text);
                Assert.Equal(value, text.Value);
                Assert.Empty(text.ChildNodes);
            }
        }

        public class Comment
        {
            [Fact]
            public void ReturnsCommentNodeInitialzed()
            {
                const string data = "some comment";
                var comment = Comment(data);
                Assert.NotNull(comment);
                Assert.Equal(data, comment.Data);
                Assert.Empty(comment.ChildNodes);
            }
        }

        public class Attribute
        {
            internal static void AssertEqual(string expectedName,
                                             string expectedValue,
                                             HtmlAttribute actualAttribute) =>
                AssertEqual(HtmlNs, expectedName, expectedValue, actualAttribute);

            internal static void AssertEqual(string expectedNamespaceUri,
                                             string expectedName,
                                             string expectedValue,
                                             HtmlAttribute actualAttribute)
            {
                Assert.Equal(expectedNamespaceUri, actualAttribute.NamespaceUri);
                Assert.Equal(string.Empty, actualAttribute.Prefix);
                Assert.Equal(expectedName, actualAttribute.Name);
                Assert.Equal(expectedValue, actualAttribute.Value);
            }

            [Fact]
            public void ReturnsAttributeInitialized()
            {
                const string name = "name";
                const string value = "value";

                AssertEqual(name, value, Attribute(name, value));
            }

            [Fact]
            public void Deconstruction()
            {
                const string name = "name";
                const string value = "value";

                var attribute = Attribute(name, value);
                var (n, v) = attribute;
                Assert.Equal(n, name);
                Assert.Equal(v, value);
            }

            [Fact]
            public void DeconstructionWithNamespace()
            {
                const string name = "name";
                const string value = "value";

                var attribute = Attribute(name, value);
                var (ns, n, v) = attribute;
                Assert.Equal(ns, HtmlNs);
                Assert.Equal(n, name);
                Assert.Equal(v, value);
            }

            [Fact]
            public void DeconstructionWithNamespaceAndPrefix()
            {
                const string name = "name";
                const string value = "value";

                var attribute = Attribute(name, value);
                var (ns, prefix, n, v) = attribute;
                Assert.Equal(ns, HtmlNs);
                Assert.Equal(prefix, string.Empty);
                Assert.Equal(n, name);
                Assert.Equal(v, value);
            }

            const string XmlnsNs = "http://www.w3.org/2000/xmlns/";

            [Fact]
            public void XmlnsWithValueOnlyReturnsXmlnsAttributeInXmlnsNamespace()
            {
                const string value = "http://www.example.com/";
                var attribute = XmlnsAttribute(value);
                AssertEqual(XmlnsNs, "xmlns", value, attribute);
            }

            [Fact]
            public void XmlnsReturnsAttributeInXmlnsNamespace()
            {
                const string name = "name";
                const string value = "value";
                var attribute = XmlnsAttribute(name, value);
                AssertEqual(XmlnsNs, name, value, attribute);
            }

            [Fact]
            public void XmlReturnsAttributeInXmlNamespace()
            {
                const string name = "name";
                const string value = "value";
                var attribute = XmlAttribute(name, value);
                AssertEqual("http://www.w3.org/XML/1998/namespace", name, value, attribute);
            }

            [Fact]
            public void XLinkReturnsAttributeInXLinkNamespace()
            {
                const string name = "name";
                const string value = "value";
                var attribute = XLinkAttribute(name, value);
                AssertEqual("http://www.w3.org/1999/xlink", name, value, attribute);
            }
        }

        public class Attributes
        {
            [Fact]
            public void ReturnsSequenceOfInitializedAttributes()
            {
                const string name1  = "name1";
                const string value1 = "value2";
                const string name2  = "name1";
                const string value2 = "value2";

                using (var e = Attributes((name1, value1), (name2, value2)).GetEnumerator())
                {
                    Assert.True(e.MoveNext());
                    Attribute.AssertEqual(name1, value1, e.Current);

                    Assert.True(e.MoveNext());
                    Attribute.AssertEqual(name2, value2, e.Current);

                    Assert.False(e.MoveNext());
                }
            }
        }

        public class Element
        {
            [Fact]
            public void ReturnsElementNodeInitialzed()
            {
                const string tagName = "element";
                var element = Element(tagName);

                Assert.NotNull(element);
                Assert.Equal(tagName, element.TagName);
                Assert.Equal(HtmlNs, element.NamespaceUri);
                Assert.Empty(element.Attributes);
                Assert.Empty(element.ChildNodes);
            }

            [Fact]
            public void WithAttributesReturnsElementNodeInitialzedSuch()
            {
                const string tagName = "element";
                const string name1 = "name1"; const string value1 = "value1";
                const string name2 = "name2"; const string value2 = "value2";

                var element =
                    Element(tagName,
                        Attribute(name1, value1),
                        Attribute(name2, value2));

                Assert.NotNull(element);
                Assert.Equal(tagName, element.TagName);
                Assert.Equal(HtmlNs, element.NamespaceUri);

                Assert.Equal(2, element.Attributes.Count);
                Attribute.AssertEqual(name1, value1, element.Attributes[0]);
                Attribute.AssertEqual(name2, value2, element.Attributes[1]);

                Assert.Empty(element.ChildNodes);
            }

            [Fact]
            public void WithAttributesAndChildrenReturnsElementNodeInitialzedSuch()
            {
                const string tagName = "element";
                const string name1  = "name1";
                const string value1 = "value2";
                const string name2  = "name1";
                const string value2 = "value2";

                var child1 = Text("text");
                var child2 = Comment("some comment");
                var child3 = Element("child");

                var element =
                    Element(tagName,
                        Attributes((name1, value1), (name2, value2)),
                        child1,
                        child2,
                        child3);

                Assert.NotNull(element);
                Assert.Equal(tagName, element.TagName);
                Assert.Equal(HtmlNs, element.NamespaceUri);

                Assert.Equal(2, element.Attributes.Count);
                Attribute.AssertEqual(name1, value1, element.Attributes[0]);
                Attribute.AssertEqual(name2, value2, element.Attributes[1]);

                Assert.Equal(3, element.ChildNodes.Count);
                Assert.Same(child1, element.ChildNodes[0]);
                Assert.Same(child2, element.ChildNodes[1]);
                Assert.Same(child3, element.ChildNodes[2]);
            }

            [Fact]
            public void WithStringReturnsElementWithStringAsTextNode()
            {
                const string tagName = "p";
                const string text = "This is a paragraph.";
                var e = Element(tagName, text);

                Assert.NotNull(e);
                Assert.Equal(tagName, e.TagName);

                Assert.Empty(e.Attributes);

                Assert.Single(e.ChildNodes);
                Assert.IsType<HtmlText>(e.FirstChild);
                Assert.Equal(text, ((HtmlText) e.FirstChild).Value);
            }

            [Fact]
            public void WithAttributesAndStringReturnsElementWithStringAsTextNode()
            {
                const string tagName = "div";
                const string text = "Please tread carefully!";
                const string @class = nameof(@class);
                const string classes = "warn";
                var e = Element(tagName, Attributes((@class, classes)), text);

                Assert.NotNull(e);
                Assert.Equal(tagName, e.TagName);
                Assert.Single(e.Attributes);

                var attr = e.Attributes.Single();
                Assert.Equal(@class, attr.Name);
                Assert.Equal(classes, attr.Value);

                Assert.Single(e.ChildNodes);
                Assert.IsType<HtmlText>(e.FirstChild);
                Assert.Equal(text, ((HtmlText) e.FirstChild).Value);
            }
        }

        public class DocumentFragment
        {
            [Fact]
            public void ReturnsDocumentFragmentInitialized()
            {
                var fragment = DocumentFragment();
                Assert.NotNull(fragment);
                Assert.Empty(fragment.ChildNodes);
            }

            [Fact]
            public void WithChildrenReturnsDocumentFragmentInitializedSuch()
            {
                var child1 = Text("text");
                var child2 = Comment("some comment");
                var child3 = Element("child");

                var fragment = DocumentFragment(child1, child2, child3);

                Assert.NotNull(fragment);

                Assert.Equal(3, fragment.ChildNodes.Count);
                Assert.Same(child1, fragment.ChildNodes[0]);
                Assert.Same(child2, fragment.ChildNodes[1]);
                Assert.Same(child3, fragment.ChildNodes[2]);
            }

            [Fact]
            public void WithSequenceOfChildrenReturnsDocumentFragmentInitializedSuch()
            {
                var child1 = Text("text");
                var child2 = Comment("some comment");
                var child3 = Element("child");

                var children = new HtmlNode[] { child1, child2, child3 };
                var fragment = DocumentFragment(children.AsEnumerable());

                Assert.NotNull(fragment);

                Assert.Equal(3, fragment.ChildNodes.Count);
                Assert.Same(child1, fragment.ChildNodes[0]);
                Assert.Same(child2, fragment.ChildNodes[1]);
                Assert.Same(child3, fragment.ChildNodes[2]);
            }
        }

        public class Template
        {
            [Fact]
            public void ReturnsTemplateNodeInitialized()
            {
                var content =
                    DocumentFragment(
                        Text("text"),
                        Comment("some comment"),
                        Element("child"));

                var template = Template(content);

                Assert.NotNull(template);
                Assert.Same(content, template.Content);
            }

            [Fact]
            public void WithAttributesReturnsTemplateNodeInitializedSuch()
            {
                const string name1  = "name1";
                const string value1 = "value2";
                const string name2  = "name1";
                const string value2 = "value2";

                var content =
                    DocumentFragment(
                        Text("text"),
                        Comment("some comment"),
                        Element("child"));

                var template = Template(Attributes((name1, value1), (name2, value2)), content);

                Assert.NotNull(template);

                Assert.Equal(2, template.Attributes.Count);
                Attribute.AssertEqual(name1, value1, template.Attributes[0]);
                Attribute.AssertEqual(name2, value2, template.Attributes[1]);

                Assert.Same(content, template.Content);
            }
        }

        public class Document
        {
            [Fact]
            public void ReturnsDocumentNodeInitialized()
            {
                HtmlElement html;
                var document = Document(html = Element("html"));

                Assert.NotNull(document);
                Assert.Single(document.ChildNodes);
                Assert.Same(html, document.ChildNodes.Single());
            }

            [Fact]
            public void WithChildrenReturnsDocumentNodeInitializedSuch()
            {
                var head =
                    Element("head",
                        Element("title", "The Document"));
                var body =
                    Element("body",
                        Element("p", "This is a paragraph."));

                var document = Document(head, body);

                Assert.NotNull(document);
                Assert.Equal(2, document.ChildNodes.Count);
                Assert.Same(head, document.ChildNodes[0]);
                Assert.Same(body, document.ChildNodes[1]);
            }
        }
    }
}
