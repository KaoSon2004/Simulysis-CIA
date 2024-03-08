using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Entities.Types
{
	public class ExcelRow
	{
        public int rowIndex;
        public List<String> lstCells;
        public byte[] hash;

        public ExcelRow(int _rowIndex)
        {
            lstCells = new List<String>();
            rowIndex = _rowIndex;
        }

        public override string ToString()
        {
            string resp;

            resp = string.Empty;

            foreach (string cellText in lstCells)
            {
                if (resp != string.Empty)
                {
                    resp = resp + "," + cellText;
                }
                else
                {
                    resp = cellText;
                }
            }
            return resp;
        }
        public void CalculateHash()
        {
            byte[] rowBytes;
            byte[] cellBytes;
            int pos;
            int numRowBytes;

            //Determine how much bytes are required to store a single excel row
            numRowBytes = 0;
            foreach (string cellText in lstCells)
            {
                numRowBytes += NumBytes(cellText);
            }

            //Allocate space to calculate the HASH of a single row

            rowBytes = new byte[numRowBytes];
            pos = 0;

            //Concatenate the cellText of each cell, converted to bytes,into a single byte array
            foreach (string cellText in lstCells)
            {
                cellBytes = GetBytes(cellText);
                System.Buffer.BlockCopy(cellBytes, 0, rowBytes, pos, cellBytes.Length);
                pos = cellBytes.Length;

            }

            hash = new MD5CryptoServiceProvider().ComputeHash(rowBytes);

        }
        static int NumBytes(string str)
        {
            return str.Length * sizeof(char);
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[NumBytes(str)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}

