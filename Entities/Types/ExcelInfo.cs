using System;
using System.Collections.Generic;
using LinqToExcel;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Entities.Types
{
	public class ExcelInfo
	{
        public List<ExcelRow> excelRows;

        public ExcelInfo()
        {
            excelRows = new List<ExcelRow>();
        }
        public bool ContainsHash(byte[] hashToLook)
        {

            bool found;

            found = false;

            foreach (ExcelRow eRow in excelRows)
            {
                found = EqualHash(eRow.hash, hashToLook);

                if (found)
                {
                    break;
                }
            }


            return found;
        }
        public static bool EqualHash(byte[] hashA, byte[] hashB)
        {
            bool bEqual;
            int i;

            bEqual = false;

            if (hashA.Length == hashB.Length)
            {
                i = 0;
                while ((i < hashA.Length) && (hashA[i] == hashB[i]))
                {
                    i++;
                }
                if (i == hashA.Length)
                {
                    bEqual = true;
                }
            }
            return bEqual;
        }
    }
}

