using System;
using System.Collections.Generic;

namespace OpenTrader.Data
{
    public partial class ReaderCode 
    {
        public string Reader;
        public string mCode;
        public string DataFileId { get; set; }
        public int Id { get; set; }

        private ReaderCodes mReaderCodes;

        public string Code
        {
            get { return mCode; }
            set
            {
                mCode = value;
                mReaderCodes.Replace(this);
            }
        }

        public ReaderCodes ReaderCodes
        {
            set { mReaderCodes = value; }
            get { return mReaderCodes; }
        }

        public ReaderCode(ReaderCodes readercodes)
        {
            this.ReaderCodes = readercodes;
        }

    }
}

