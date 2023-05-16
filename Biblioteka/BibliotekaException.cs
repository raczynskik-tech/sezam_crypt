using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    public class BibliotekaException : Exception { }

    public class NoFilesInDirException : BibliotekaException { }

    public class DirectoryExistsException : BibliotekaException { }

    public class InvalidFilePathToProcessException : BibliotekaException { }

    public class FileNotExistsException : BibliotekaException { }
}
