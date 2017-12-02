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

    partial class HtmlAttributeName
    {
        public static readonly Dictionary<string, HtmlAttributeName> CommonNames =
            new[]
            {
                "id",
                "name",
                "class",
                "style",
                "href",
                "type",
                "src",
                "alt",
                "title",
                "rel",
                "dir",
                "lang",
                "accesskey",
                "onabort",
                "onblur",
                "onchange",
                "onclick",
                "oncontextmenu",
                "ondblclick",
                "ondrag",
                "ondragend",
                "ondragenter",
                "ondragleave",
                "ondragover",
                "ondragstart",
                "ondrop",
                "onerror",
                "onfocus",
                "onkeydown",
                "onkeypress",
                "onkeyup",
                "onload",
                "onmousedown",
                "onmousemove",
                "onmouseout",
                "onmouseover",
                "onmouseup",
                "onmousewheel",
                "onreadystatechange",
                "onscroll",
                "onselect",
                "onsubmit",
            }
            .ToDictionary(n => n,
                          n => new HtmlAttributeName(n),
                          StringComparer.OrdinalIgnoreCase);
    }
}
