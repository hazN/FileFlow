using Xunit;

namespace FileFlow.Tests
{

    public class FolderNameValidationTests
    {
        [Theory]
        [InlineData("../etc")]
        [InlineData("..\\Windows")]
        [InlineData("folder/../../secret")]
        public void InvalidFolderNames_ContainingPathTraversal_AreRejected(string maliciousName)
        {
            bool isValid = FileFlow.API.Validation.FolderNameValidator.IsValid(maliciousName);
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("Documents")]
        [InlineData("My Photos 2024")]
        [InlineData("Project_Files")]
        public void ValidFolderNames_AreAccepted(string validName)
        {
            bool isValid = FileFlow.API.Validation.FolderNameValidator.IsValid(validName);
            Assert.True(isValid);
        }
    }
}