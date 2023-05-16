using Microsoft.VisualStudio.TestTools.UnitTesting;
using Biblioteka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Specialized;
using System.Configuration;

namespace Biblioteka.Tests
{
    [TestClass()]
    public class EncryptDecryptTests
    {
        string rootPath;
        string emptyDir;
        string notEmptyDir;
        string rootFileName;
        string fileNameToDelete;
        string fileFrom;
        string fileTo;

        [TestInitialize]
        public void Initialize()
        {
            //Odczytanie danych z pliku konfiguracyjnego
            NameValueCollection nameValueCollection = ConfigurationManager.AppSettings;
            rootPath = nameValueCollection["rootPath"];
            emptyDir = nameValueCollection["emptyDir"];
            notEmptyDir = nameValueCollection["notEmptyDir"];
            rootFileName = nameValueCollection["rootFileName"];
            fileNameToDelete = nameValueCollection["toDeleteFileName"];
            fileFrom = nameValueCollection["fileFrom"];
            fileTo = nameValueCollection["fileTo"];

            //Zawartość plików testowych
            string data = "testTESTtest";
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            //Przygotowanie struktury plików
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath); //Katalog korzeń
                Directory.CreateDirectory(Path.Combine(rootPath, emptyDir)); //Pusty katalog
                Directory.CreateDirectory(Path.Combine(rootPath, notEmptyDir)); //Katalog z plikami
                for(int i = 0; i < 3; i++) //Pliki puste
                {
                    using (FileStream fs = File.Create(Path.Combine(rootPath, notEmptyDir, $"{rootFileName}{i}.txt"))) { }
                }
                using (FileStream fs = File.Create(Path.Combine(rootPath, $"{rootFileName}.txt"))) //Plik z zawartością
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
                using (File.Create(Path.Combine(rootPath, fileNameToDelete))) { } //Plik do usunięcia
                using (File.Create(Path.Combine(rootPath, fileFrom))) { } //Plik do przeniesienia
            }
        }

        [TestMethod()]
        public void getFilesFromDirTest()
        {
            string path = Path.Combine(rootPath, emptyDir);

            Assert.ThrowsException<NoFilesInDirException>(() => EncryptDecrypt.getFilesFromDir(path), "Testowana ścieżka jest ścieżką do niepustego katalogu");
        }

        [TestMethod()]
        public void getFilesFromDirTest2()
        {
            string path = Path.Combine(rootPath, notEmptyDir);
            List<FileInfo> files = EncryptDecrypt.getFilesFromDir(path);

            int expectedCount = 3;
            int resultCount = files.Count;

            Assert.AreEqual(expectedCount, resultCount, "Katalog nie zawiera odpowiedniej liczby plików wewnątrz");
        }

        [TestMethod()]
        public void getFilesFromDirTest3()
        {
            Assert.ThrowsException<ArgumentNullException>(() => EncryptDecrypt.getFilesFromDir(null), "Podana ścieżka to katalog");
        }

        [TestMethod()]
        public void preparePathToDecryptTest()
        {
            string testPath = "to_jest_testowa_sciezka.txtsc";
            string expectedPath = "to_jest_testowa_sciezka.txt";

            string resultPath = EncryptDecrypt.preparePathToDecrypt(testPath);

            Assert.AreEqual(expectedPath, resultPath, "Podane przykładowe ścieżki są różne");
        }

        [TestMethod()]
        public void preparePathToDecryptTest2()
        {
            string testPath = "to_jest_testowa_sciezka.txts";

            Assert.ThrowsException<InvalidFilePathToProcessException>(() => EncryptDecrypt.preparePathToDecrypt(testPath), "Testowana ścieżka może zostać zmodyfikowana, aby utworzyć poprawną ścieżkę do rozszyfrowanego pliku");
        }

        [TestMethod()]
        public void preparePathToEncryptTest()
        {
            string testPath = "to_jest_testowa_sciezka.txt";
            string expectedPathEnd = ".txtsc";

            string resultPath = "";
            try
            {
                resultPath = EncryptDecrypt.preparePathToEncrypt(testPath);
                StringAssert.EndsWith(resultPath, expectedPathEnd, "Przygotowana ścieżka jest niepoprawna");
            }
            catch (AssertFailedException ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod()]
        public void preparePathViceVersaTest()
        {
            string testPath = "to_jest_testowa_sciezka.txt";
            string expectedPath = "to_jest_testowa_sciezka.txt";

            string resultPath = EncryptDecrypt.preparePathToDecrypt(EncryptDecrypt.preparePathToEncrypt(testPath));

            Assert.AreEqual(expectedPath, resultPath, "Przygotowana ścieżka jest niepoprawna");
        }

        [TestMethod()]
        public void deleteFileTest()
        {
            string testPath = Path.Combine(rootPath, emptyDir, "to_jest_testowa_sciezka.txt");
            
            Assert.ThrowsException<FileNotExistsException>(() => EncryptDecrypt.deleteFile(testPath), "Plik istnieje");
        }

        [TestMethod()]
        public void deleteFileTest2()
        {
            string testPath = Path.Combine(rootPath, fileNameToDelete);

            bool isDeleted = EncryptDecrypt.deleteFile(testPath);
            
            Assert.IsTrue(isDeleted, "Plik nie został usunięty");
        }

        [TestMethod()]
        public void encryptDecryptTest()
        {
            string testPathToEncrypt = Path.Combine(rootPath, $"{rootFileName}.txt");
            string testPathToDecrypt = Path.Combine(rootPath, $"{rootFileName}.txtsc");
            string testPathToRead = Path.Combine(rootPath, $"{rootFileName}.txt");

            FileInfo filesrc = new FileInfo(testPathToEncrypt);
            string fileExpected = string.Empty;
            using (StreamReader streamReader = new StreamReader(filesrc.OpenRead()))
            {
                fileExpected = streamReader.ReadToEnd();
            }
            EncryptDecrypt.encryptFile(filesrc);
            EncryptDecrypt.deleteFile(filesrc.FullName);

            FileInfo filedest = new FileInfo(testPathToDecrypt);
            EncryptDecrypt.decryptFile(filedest);

            string fileResult = string.Empty;
            FileInfo fileProcessed = new FileInfo(testPathToRead);
            using (StreamReader streamReader = new StreamReader(fileProcessed.OpenRead()))
            {
                fileResult = streamReader.ReadToEnd();
            }

            Assert.AreEqual(fileExpected, fileResult, "Przetworzona wiadomość różni się od oryginału");
        }

        [TestMethod()]
        public void moveFileTest()
        {
            string srcPath = Path.Combine(rootPath, fileFrom);
            string destPath = Path.Combine(rootPath, fileTo);

            bool result = EncryptDecrypt.moveFile(srcPath, destPath);

            Assert.IsTrue(result, "Plik nie został przeniesiony pod inną ścieżkę");
        }


        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, true);
            }
        }
    }
}