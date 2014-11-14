using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class FileContainer : Object
    {
        private string _fileTitle;

        public string FileTitle
        {
            get { return _fileTitle; }
            set { _fileTitle = value; }
        }

        private string _filePath;

        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; }
        }
    }
}