﻿using System;
using System.ComponentModel;
using System.Globalization;

namespace HIC.Common.Validation.Constraints.Secondary
{
    public class BoundDate : Bound
    {
        [Description("Optional, Requires the value being validated to be AFTER this date")]
        public DateTime? Lower { get; set; }
        [Description("Optional, Requires the value being validated to be BEFORE this date")]
        public DateTime? Upper { get; set; }
        
        public BoundDate()
        {
            Inclusive = true;
        }
        
        public override ValidationFailure Validate(object value, object[] otherColumns, string[] otherColumnNames)
        {
            if(value == null)
                return null;

            if (value is string)
            {
                value = SafeConvertToDate(value as string);
            
                if (!((DateTime?)value).HasValue)
                    return null;
            }

            var d = (DateTime)value;

            if (value != null && !IsWithinRange(d)) 
                return new ValidationFailure(CreateViolationReportUsingDates(d),this);
            
            if (value != null && !IsWithinRange(d,otherColumns, otherColumnNames))
                return new ValidationFailure(CreateViolationReportUsingFieldNames(d),this);

            return null;
        }

        private bool IsWithinRange(DateTime d)
        {
            if (Inclusive)
            {
                if(Lower != null)
                    if (d < Lower)
                        return false;

                if(Upper != null)
                    if (d > Upper)
                        return false;
            }
            else
            {
                if (Lower != null)
                  if (d <= Lower)
                    return false;

                if (Upper != null)
                    if (d >= Upper)
                        return false;
            }

            return true;
        }

        private bool IsWithinRange(DateTime d, object[] otherColumns, string[] otherColumnNames)
        {
            DateTime? low = SafeConvertToDate(LookupFieldNamed(LowerFieldName, otherColumns, otherColumnNames));
            DateTime? up = SafeConvertToDate(LookupFieldNamed(UpperFieldName, otherColumns, otherColumnNames));

            if (Inclusive)
            {
                if (low.HasValue && d < low.Value)
                    return false;

                if (up.HasValue && d > up.Value)
                    return false;
            }
            else
            {
                if (low.HasValue && d <= low.Value)
                    return false;

                if (up.HasValue && d >= up.Value)
                    return false;
            }

            return true;
        }

        private DateTime? SafeConvertToDate(object lookupFieldNamed)
        {
            if (lookupFieldNamed == null)
                return null;

            if (lookupFieldNamed == DBNull.Value)
                return null;

            if (lookupFieldNamed is DateTime)
                return (DateTime)lookupFieldNamed;

            if (lookupFieldNamed is string)
            {
                if (string.IsNullOrWhiteSpace(lookupFieldNamed as string))
                    return null; 
                else
                    try
                    {
                        lookupFieldNamed = DateTime.Parse(lookupFieldNamed as string);
                    }
                    catch (InvalidCastException )
                    {
                        return null; //it's not our responsibility to look for malformed dates in this constraint (leave that to primary constraint date)
                    }
                    catch (FormatException )
                    {
                        return null;
                    }

                return (DateTime)lookupFieldNamed;
            }

            throw new ArgumentException("Did not know how to deal with object of type " +
                                        lookupFieldNamed.GetType().Name);
        }

        private string CreateViolationReportUsingDates(DateTime d)
        {
            if (Lower != null && Upper != null)
                return BetweenMessage(d, Lower.ToString(), Upper.ToString());

            if (Lower != null)
                return GreaterThanMessage(d, Lower.ToString());

            if (Upper!= null)
                return LessThanMessage(d, Upper.ToString());

            throw new InvalidOperationException("Illegal state.");
        }

        private string CreateViolationReportUsingFieldNames(DateTime d)
        {
            if (!String.IsNullOrWhiteSpace(LowerFieldName) && !String.IsNullOrWhiteSpace(UpperFieldName)) 
                return BetweenMessage(d, LowerFieldName, UpperFieldName);

            if (!String.IsNullOrWhiteSpace(LowerFieldName))
                return GreaterThanMessage(d, LowerFieldName);

            if (!String.IsNullOrWhiteSpace(UpperFieldName))
                return LessThanMessage(d, UpperFieldName);

            throw new InvalidOperationException("Illegal state.");
        }

        private string BetweenMessage(DateTime d, string l, string u)
        {
            return "Date " + Wrap(d.ToString(CultureInfo.InvariantCulture)) + " out of range. Expected a date between " + Wrap(l) + " and " + Wrap(u) + (Inclusive ? " inclusively" : " exclusively") + ".";
        }

        private string GreaterThanMessage(DateTime d, string s)
        {
            return "Date " + Wrap(d.ToString(CultureInfo.InvariantCulture)) + " out of range. Expected a date greater than " + Wrap(s) + ".";
        }

        private string LessThanMessage(DateTime d, string s)
        {
            return "Date " + Wrap(d.ToString(CultureInfo.InvariantCulture)) + " out of range. Expected a date less than " + Wrap(s) + ".";
        }

        private string Wrap(string s)
        {
            return "[" + s + "]";
        }
        
        public override string GetHumanReadableDescriptionOfValidation()
        {
            string result = "Checks that a date is within a given set of bounds.  This field is currently configured to be ";
            
            if (Lower != null )
                if(Inclusive)
                    result += " >=" + Lower;
                else
                    result += " >" + Lower;
            
            if(Upper != null)
                if (Inclusive)
                    result += " <=" + Upper;
                else
                    result += " <" + Upper;

            return result;
        }
    }
}