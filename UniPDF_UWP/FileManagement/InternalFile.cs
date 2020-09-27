﻿using iText.Kernel.Pdf;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace UniPDF_UWP.FileManagement
{
    /// <summary>
    /// Internal representation of a Pdf File: 
    /// </summary>
    public class InternalFile
    {
        private PdfDocument document;

        public StorageFile File { get; }
        public FileSize Size { get; private set; }

        /// <summary>
        /// Document is only accessible in case it is not encrypted or a valid key has been provided to decrypt the file.
        /// </summary>
        public PdfDocument Document
        {
            get
            {
                if (!Decrypted)
                {
                    throw new EncryptedFileException("File is encrypted. Cannot access the PDF document");
                }
                return document;
            }
            private set => document = value;
        }
        public string FileName { get; private set; }
        public string FileSize { get; private set; }
        public int PageCount { get; private set; }
        public bool Decrypted { get; private set; }

        /// <summary>
        /// Initializes an internal file instance: Reads the file's size and tries to open the document.
        /// </summary>
        private InternalFile(StorageFile storageFile)
        {
            File = storageFile ?? throw new ArgumentException("Cannot retrieve file properties from null-reference. Expected StorageFile instance.");
            FileName = File.Name;
            GetFileSize();
        }

        /// <summary>
        /// API for creating and loading a new StorageFile
        /// </summary>
        /// <returns></returns>
        public static async Task<InternalFile> LoadInternalFileAsync(StorageFile storageFile)
        {
            InternalFile internalFile = new InternalFile(storageFile);
            await internalFile.OpenDocument();
            return internalFile;
        }

        /// <summary>
        /// The method returns true if the document is alredy decrypted or if it can be decrypted using the given key. 
        /// In both cases, the method ensures that the PdfDocument instance is loaded and accessible in the future.
        /// If the document is encrypted and the given key cannot be used for decrypting the file, the method returns false.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        //public bool TryDecrypting(string key)
        //{
        //    if (!Decrypted)
        //    {
        //        // transform the key into a password and set the reader properties
        //        byte[] password = new System.Text.ASCIIEncoding().GetBytes(key);
        //        ReaderProperties readerProperties = new ReaderProperties().SetPassword(password);
        //        PdfReader inputReader = new PdfReader(inputStream, readerProperties);

        //        try
        //        {
        //            // try to open the document. If successful, the key is valid
        //            Document = new PdfDocument(inputReader);
        //        }
        //        catch
        //        {
        //            Decrypted = false;
        //            inputReader.Close();
        //        }
        //        Decrypted = true;
        //    }
        //    return Decrypted;
        //}

        private async void GetFileSize()
        {
            var basicProperties = await File.GetBasicPropertiesAsync();
            ulong length = basicProperties.Size;
            Size = new FileSize(length);
            FileSize = Size.ToString();
        }

        /// <summary>
        /// Opens the corresponding PdfDocument of this instances attribute storage file.
        /// Throws an EncryptedFileException in case the document is unexpectedly encrypted and thus cannot be opened without a valid key.
        /// </summary>
        private async Task OpenDocument()
        {
            var inputStream = await File.OpenStreamForReadAsync();
            PdfReader inputReader = new PdfReader(inputStream);
            try
            {
                // try, whether the PdfDocumet can be opened without a key
                Document = new PdfDocument(inputReader);            
            }
            catch
            {
                PageCount = 0;
                Decrypted = false;
                inputReader.Close();
                throw new EncryptedFileException("Selected file is encrypted. Cannot access the file without a valid password.");
            }

            Decrypted = true;
            PageCount = Document.GetNumberOfPages();
        }
    }

    /// <summary>
    /// Exception expresses, that a document is encrypted and thus cannot be opened without providing the valid key.
    /// </summary>
    public class EncryptedFileException : Exception
    {
        public EncryptedFileException(string message) : base(message) { }
    }
}