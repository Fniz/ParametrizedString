using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToConstant.Local
namespace Fniz.ParametrizedString.Tests
{
    [TestFixture]
    public class ParametrizedStringBuilderTests
    {
        [TestCase("$Drive$","Drive")]
        [TestCase("$Directory$", "Directory")]
        public void Should_Return_Parameter_Names_Of_The_String(string variable, string r)
        {
            // Act
            string s = variable + "\\Mes Documents";

            // Arrange
            string expectedResult = r;
            string result = s.GetParameterNames('$').FirstOrDefault();

            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        [TestCase('%')]
        [TestCase('*')]
        [TestCase('_')]
        [TestCase('!')]
        public void Should_Support_Many_Delimiters(char delimiter )
        {
            // Act
            string s = delimiter + "Drive" + delimiter + "\\Mes Documents";

             // Arrange
            string expectedResult = "Drive";
            string result = s.GetParameterNames(delimiter).FirstOrDefault();

            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        [TestCase('(',')')]
        [TestCase('[',']')]
        [TestCase('{','}')]
        public void Should_Support_Different_Starting_Delimiter_Char_And_Ending_Delimiter_Char(char startDelimiter, char endDelimiter)
        {
            // Act
            string s = startDelimiter + "Drive" + endDelimiter+"\\Mes Documents";

            // Arrange
            string expectedResult = "Drive";
            string result = s.GetParameterNames(startDelimiter, endDelimiter).FirstOrDefault();
            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Should_Throws_An_Argument_Exception_When_More_2_delimiters_Chars_Are_passed()
        {
            string s = "{Drive}\\MesDocuments";
            Assert.That(() => s.GetParameterNames(',','(',')'),
                Throws.Exception
                  .TypeOf<ArgumentOutOfRangeException>()
                  .With.Property("ParamName")
                  .EqualTo("delimiterChars"));
        }


        [Test]
        public void Should_Returns_3_Names_Parameter_With_Single_Delimiter()
        {
            // Act
            string s = "$Drive$\\$Directory$_$File$";

            // Arrange
            var expectedResult = new List<string> {"Drive", "Directory" , "File"};
            var result = s.GetParameterNames('$').ToList();

            // Assert
            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void Should_Returns_3_Names_Parameter_With_2_Delimiters()
        {
            // Act
            string s = "{Drive}\\{Directory}_{File}";

            // Arrange
            var expectedResult = new List<string> { "Drive", "Directory", "File" };
            var result = s.GetParameterNames('{','}').ToList();

            // Assert
            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void Allow_To_Set_A_Parameter()
        {
            // Act
            string s = "{Drive}\\{Directory}_{File}";

            s.GetParameterNames('{', '}');
            s.SetParameter("Drive", "C:");

            string result = s.Resolve();
            string expectedResult = "C:\\{Directory}_{File}";
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Allow_To_Set_3_Parameters()
        {
            // Act
            string s = "{Drive}\\{Directory}_{File}";

            s.GetParameterNames('{', '}');
            s.SetParameter("Drive","C:");
            s.SetParameter("Directory", "Mes Documents");
            s.SetParameter("File", "File.txt");

            string result = s.Resolve();

            string expectedResult = "C:\\Mes Documents_File.txt";
            Assert.AreEqual(expectedResult, result);
        }


        [Test]
        public void Should_Support_String_Separator()
        {
            // Act
            string s = "$$Drive$$\\$$Directory$$_$$File$$";

            // Arrange
            var expectedResult = new List<string> { "Drive", "Directory", "File" };
            var result = s.GetParameterNames("$$").ToList();

            // Assert
            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void Should_Support_String_Separator_With_Different_Chars()
        {
            // Act
            string s = "$*/Drive$*/\\$*/Directory$*/_$*/File$*/";

            // Arrange
            var expectedResult = new List<string> { "Drive", "Directory", "File" };
            var result = s.GetParameterNames("$*/").ToList();

            // Assert
            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void Should_Support_String_Separator_With_Different_String()
        {
            // Act
            string s = "{{Drive}}\\{{Directory}}_{{File}}";

            // Arrange
            var expectedResult = new List<string> { "Drive", "Directory", "File" };
            var result = s.GetParameterNames("{{","}}").ToList();

            // Assert
            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void Should_Support_Parameters_Side_By_Side_And_Returns_Parameters_Without_Escaped_String()
        {
            // Act
            string s = "**Drive****Directory****File**";

            // Arrange
            var expectedResult = new List<string> { "Drive**Directory**File" };
            var result = s.GetParameterNames("**").ToList();

            // Assert
            CollectionAssert.AreEquivalent(expectedResult, result);
        }

        [Test]
        public void Should_Set_Keys()
        {
            // Act
            string s = "{{Drive}}\\{{Directory}}_{{File}}";

            // Arrange
            //s.GetParameterNames("{{", "}}");

            s.SetParameter("Drive", "C:");
            s.SetParameter("Directory", "Windows");
            s.SetParameter("File", "myFile.doc");

            string expectedResult = "C:\\Windows_myFile.doc";
            string result = s.Resolve("{{", "}}");

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Should_Return_The_String_If_No_Parameter_Are_Found()
        {
            // Act
            string s = "{{Drive}}\\{{Directory}}_{{File}}";
            // Arrange
            s.GetParameterNames("$");

            string expectedResult = s;
            string result = s.Resolve();

            Assert.AreEqual(result, expectedResult);
        }

        [Test]
        public void Should_Throws_An_Exception_If_String_Is_Badly_Formatted()
        {
            // Act
            // Miss a $ after $Directory.
            string s = "$Drive$\\$Directory_$File$";

            Assert.That(() => s.GetParameterNames('$'),
                        Throws.Exception
                            .TypeOf<FormatException>());
        }

        [Test]
        public void Should_Be_Used_With_Multiple_Instance_Of_String()
        {
            // Act
            // Miss a $ after $Directory.
            string s = "$Drive$\\$Directory$_$File$";
            string s2 = "{Volume$\\{Repertoire$_{File$";

            s.SetParameter("Drive", "C:");
            s.SetParameter("Directory", "Mes Documents");
            s.SetParameter("File", "MyFile.docx");

            s2.SetParameter("Volume", "C:");
            s2.SetParameter("Repertoire", "Mes Documents");
            s2.SetParameter("File", "MyFile.docx");

            string r1 = s.Resolve('$');
            string r2 = s2.Resolve('{','$');
            Assert.AreEqual(r1,r2);
        }

        [Test]
        public void Should_Ignore_Escaped_String()
        {
            string s = "$*Drive$*$*$*";
            s.SetParameter("Drive$*", "C:");

            string expectedResult = "C:";
            string result = s.Resolve("$*");

            Assert.AreEqual(expectedResult, result);
        }
    }
}
