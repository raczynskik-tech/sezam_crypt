using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.CodeDom;
using System.IO.Pipes;
using System.IO.Compression;

namespace Biblioteka
{
    public static class EncryptDecrypt
    {
        public static byte[] key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        public static List<FileInfo> getFilesFromDir(string dirPath)
        {
            try
            {
                var files = new List<FileInfo>();
                DirectoryInfo directory = new DirectoryInfo(dirPath);
                files = directory.GetFiles().ToList();

                if (files.Count == 0)
                    throw new NoFilesInDirException();
                
                return files;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public static string preparePathToEncrypt(string path)
        {
            return path + "sc";
        }

        public static string preparePathToDecrypt(string path)
        {
            if (path.EndsWith(".txtsc"))
            {
                int index = path.LastIndexOf("sc");
                return path.Substring(0, index);
            }
            else
                throw new InvalidFilePathToProcessException();
        }

        public static string encryptFile(FileInfo file)
        {
            try
            {
                string newFilePath = preparePathToEncrypt(file.FullName); //Przygotowanie ścieżki dla nowego pliku

                using (Aes aes = Aes.Create())
                {
                    //byte[] key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
                    aes.Key = key;
                    byte[] iv = aes.IV;

                    using(FileStream outputFileStream = new FileStream(newFilePath, FileMode.Create))
                    {
                        outputFileStream.Write(iv, 0, iv.Length); //Zapis 'klucza'

                        using (CryptoStream cryptoStream = new CryptoStream(outputFileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            using(FileStream inputFileStream = new FileStream(file.FullName, FileMode.Open))
                            {
                                int dane;
                                while((dane = inputFileStream.ReadByte()) != -1)
                                {
                                    cryptoStream.WriteByte((byte)dane);
                                }
                            }
                        }
                    }
                }
                return newFilePath;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public static string decryptFile(FileInfo file)
        {
            try
            {
                string outputFilePath = preparePathToDecrypt(file.FullName);

                using (Aes aes = Aes.Create())
                {
                    byte[] iv = new byte[aes.IV.Length];
                    int numBytesToRead = aes.IV.Length;
                    int numBytesRead = 0;
                    using (FileStream inputFileStream = new FileStream(file.FullName, FileMode.Open)) 
                    {
                        while (numBytesToRead > 0)
                        {
                            int n = inputFileStream.Read(iv, numBytesRead, numBytesToRead);
                            if (n == 0) break;

                            numBytesRead += n;
                            numBytesToRead -= n;
                        }

                        //byte[] key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };

                        using (CryptoStream cryptoStream = new CryptoStream(inputFileStream, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
                        {
                            using(FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create))
                            {
                                int dane;
                                while((dane = cryptoStream.ReadByte()) != -1)
                                {
                                    outputFileStream.WriteByte((byte)dane);
                                }
                            }

                        }
                    }
                }
                return outputFilePath;
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public static bool deleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                else
                    throw new FileNotExistsException();
            }
            catch(Exception exception)
            {
                throw exception;
            }
        }

        public static bool moveFile(string sourcePath, string destPath)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    File.Move(sourcePath, destPath);
                    return true;
                }
                else
                    throw new FileNotExistsException();
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }
    }
}
