﻿using System;
using System.Text.RegularExpressions;

namespace HIC.Common.Validation.Constraints.Primary
{
    public class Chi : PrimaryConstraint
    {
        public override ValidationFailure Validate(object value)
        {
            if (value == null)
                return null;


            var valueAsString = value as string;

            if(valueAsString == null)
                return new ValidationFailure("Incompatible type, CHIs must be strings, value passed was of type " + value.GetType().Name,this);

            string reason;

            if (!IsValidChi(valueAsString, out reason))
                return new ValidationFailure(reason,this);
           
            return null;
        }

        
        public override void RenameColumn(string originalName, string newName)
        {
            
        }

        public override string GetHumanReadableDescriptionOfValidation()
        {
            return
                "Checks that the input value is 10 characters long and the first 6 characters are a valid date and that the final digit checksum matches";
        }

        public static bool IsValidChi(string columnValueAsString, out string reason)
        {
            if (columnValueAsString.Length != 10)
            {
                reason = "CHI was not 10 characters long";
                return false;
            }

            string dd = columnValueAsString.Substring(0, 2);
            string mm = columnValueAsString.Substring(2, 2);
            string yy = columnValueAsString.Substring(4, 2);

            DateTime outDt;
            //maybe tryparse instead
            if (DateTime.TryParse(dd + "/" + mm + "/" + yy, out outDt) == false)
            {
                reason = "First 6 numbers of CHI did not constitute a valid date";
                return false;
            }


            if (columnValueAsString.Substring(columnValueAsString.Length - 1) != GetCHICheckDigit(columnValueAsString))
            {
                reason = "CHI check digit did not match";
                return false;
            }

            reason = null;
            return true;

        }

        /// <summary>
        /// Copied from CIB_Systems Connection class, it does the CHI check digit calculation
        /// </summary>
        /// <param name="sCHI"></param>
        /// <returns></returns>
        private static string GetCHICheckDigit(string sCHI)
        {
            int sum = 0, c = 0, lsCHI = 0;

            //sCHI = "120356785";
            lsCHI = sCHI.Length; // Must be 10!!

            sum = 0;
            c = (int)'0';
            for (int i = 0; i < lsCHI - 1; i++)
                sum += ((int)(sCHI.Substring(i, 1)[0]) - c) * (lsCHI - i);
            sum = sum % 11;

            c = 11 - sum;
            if (c == 11) c = 0;

            return ((char)(c + (int)'0')).ToString();

        }

        /// <summary>
        /// Return the sex indicated by the supplied CHI
        /// </summary>
        /// <param name="chi"></param>
        /// <returns>1 for male and 0 for female</returns>
        public int GetSex(string chi)
        {
            string errorReport;

            if (!IsValidChiNumber(chi, out errorReport))
                throw new ArgumentException("Invalid CHI");

            char sexChar = chi[8];

            return (int)(sexChar % 2);
        }

        /// <summary>
        /// Check the validity of the supplied CHI
        /// </summary>
        /// <param name="strChi"></param>
        /// <returns>true if the CHI is valid, false otherwise</returns>
        public static bool IsValidChiNumber(string strChi, out string errorReport)
        {
            errorReport = "Not yet implemented";

            if (!isWellFormedChi(strChi))
                return false;

            // Value of 10 indicates a checksum error
            int checkDigit = ComputeChecksum(strChi);

            return (checkDigit != 10 && (int)Char.GetNumericValue(strChi[9]) == checkDigit);
        }

        private static bool isWellFormedChi(string strChi)
        {
            if (strChi == null || strChi.Length != 10)
                return false;

            var r = new Regex("^[0-9]{10}$");
            if (!r.IsMatch(strChi))
                return false;

            return true;
        }

        private static int ComputeChecksum(string chi)
        {
            int sum = SumDigits(chi);
            int checkDigit = 0;

            int n = (11 - (sum % 11));
            if (n < 10)
                checkDigit = n;

            return checkDigit;
        }

        private static int SumDigits(string chi)
        {
            int sum = 0;
            int factor = 10;
            for (int i = 0; i < 9; i++)
            {
                sum += (chi[i] - 48) * factor--;
            }

            return sum;
        }

    }
}