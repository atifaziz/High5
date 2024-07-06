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
    using System.Collections.Generic;
    using Jacob;
    using Xunit;

    public class SerializationTests
    {
        #pragma warning disable xUnit1026 // Theory method 'Test' on test class 'SerializationTests' does not use parameter 'name'.

        [Theory, MemberData(nameof(GetTestData))]
        public void Test(string name, string input, string expected)
        {
            var document = Parser.Parse(input);
            var result = document.Serialize();
            Assert.Equal(expected, result);
        }

        #pragma warning restore xUnit1026

        public static IEnumerable<object[]> GetTestData()
        {
            var dataReader =
                JsonReader.Array(
                    JsonReader.Object(
                        JsonReader.Property("name", JsonReader.String()),
                        JsonReader.Property("input", JsonReader.String()),
                        JsonReader.Property("expected", JsonReader.String()),
                        (name, input, expected) => new object[] { name, input, expected }));

            return dataReader.Read(ThisAssembly.Resources.data.serialization.tests.Text);
        }
    }
}
