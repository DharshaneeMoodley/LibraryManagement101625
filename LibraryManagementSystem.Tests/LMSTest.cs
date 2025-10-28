using Xunit;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using System.Text;


namespace LibraryManagementSystem.Tests
{
    public class LMSTest
    {
        [Fact]
        public void Test1_AddBook_Successful()
        {
            //Get initial books count
            var initialCount = BookDataStore.GetAllBooks().Count;

            //Create a new book
            var newBook = new Book
            {
                Title = "Test Book for Add",
                Author = "Test AUthor",
                Category = "Programming",
                ISBN = "789-541654-515",
                Description = "This is a test description",
                SubmittedBy = "Test User"
            };

            //perform the action
            BookDataStore.AddBook(newBook);

            //Get the new count and verify book was added
            var newCount = BookDataStore.GetAllBooks().Count;
            Assert.Equal(initialCount +1, newCount);

            Assert.True(newBook.Id > 0, "Book should have an ID assigned");

            Assert.Equal(BookStatus.Pending, newBook.Status);

            //Verify if we can retrieve the book
            var retrievedBook = BookDataStore.GetBookById(newBook.Id);
            Assert.NotNull(retrievedBook);
            Assert.Equal("Test Book for Add", retrievedBook.Title);

        }

        [Fact]
        public async Task Test2_EncryptionFile_Successful()
        {
            var originalContent = "This is a secret file content that should be encrypted";
            var originalBytes = Encoding.UTF8.GetBytes(originalContent);
            var inputStream = new MemoryStream(originalBytes);
            var tempFile = Path.GetTempFileName();
            var encryptionService = new FileEncryptionService();

            try
            {
                await encryptionService.EncryptFileAsync(inputStream, tempFile);

                Assert.True(File.Exists(tempFile), "Encrypted file should exist");

                //Read the encrypted file
                var encryptedBytes = await File.ReadAllBytesAsync(tempFile);

                //Verify that the encrypted data is not the same as the original
                Assert.NotEqual(originalBytes, encryptedBytes);

                //Verify the encrypted file have content
                Assert.True(encryptedBytes.Length > 0, "Encrypted file should have content");

                //Verify we cannot read the original text from the encrypted file
                var encryptedText = Encoding.UTF8.GetString(encryptedBytes);
                Assert.DoesNotContain("This is a secret file content that should be encrypted", encryptedText);

            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }

        }

        [Fact]
        public async Task Test3_DecryptFile_Successful()
        {
            //Create and encrypt the file
            var originalContent = "This is a secret document";
            var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
            var tempFile = Path.GetTempFileName();
            var encryptionService = new FileEncryptionService();
            
            try
            {
                //encrypt
                await encryptionService.EncryptFileAsync(inputStream, tempFile);

                //Decrypt
                var decryptedStream = await encryptionService.DecryptFileAsync(tempFile);
                var decryptedContent = Encoding.UTF8.GetString(decryptedStream.ToArray());

                //Verify decrypted content matched the original
                Assert.Equal(originalContent, decryptedContent);
                Assert.Contains(originalContent, decryptedContent);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void Test4_ApproveBook()
        {
            var newBook = new Book
            {
                Title = "Book to Approve",
                Author = "Author Name",
                SubmittedBy = "Librarian",
                Status = BookStatus.Pending,
            };

            BookDataStore.AddBook(newBook);

            //Action : perform book approve
            var success = BookDataStore.UpdateBookStatus(newBook.Id, BookStatus.Approved, "Admin User", "Book approved, good quality");

            Assert.True(success, "Update should succeed");

            var updateBook = BookDataStore.GetBookById(newBook.Id);
            Assert.Equal(BookStatus.Approved, updateBook.Status);
            Assert.Equal("Admin User", updateBook.ReviewedBy);
            Assert.NotNull(updateBook.ReviewedDate);

        }


        [Fact]
        public void Test5_DeclineBook() 
        {

            var newBook = new Book
            {
                Title = "Book to Approve",
                Author = "Author Name",
                SubmittedBy = "Librarian",
                Status = BookStatus.Pending,
            };
            BookDataStore.AddBook(newBook);

            var success = BookDataStore.UpdateBookStatus(newBook.Id, BookStatus.Declined, "Admin User", "Book declined, bad quality");


            Assert.True(success, "Update should succeed");

            var updateBook = BookDataStore.GetBookById(newBook.Id);
            Assert.Equal(BookStatus.Declined, updateBook.Status);
            Assert.Equal("Admin User", updateBook.ReviewedBy);
            Assert.NotNull(updateBook.ReviewedDate);
        }

    }
}
