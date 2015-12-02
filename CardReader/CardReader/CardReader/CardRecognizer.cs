using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CardReader
{
    public static class CardRecognizer
    {
        public static Dictionary<RecognitionType, string> expressions = new Dictionary<RecognitionType, string>()
        {  
            // regex taken from MSDN: http://msdn.microsoft.com/en-us/library/01escwtf(v=vs.110).aspx  
            {
                RecognitionType.Email,
                @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))"+
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$"
            },  
                
            // regex taken from regex lib: http://regexlib.com/REDetails.aspx?regexp_id=296  
            {
                RecognitionType.WebPage,
                @"^(https?:\/\/)?([\w\d-_]+)\.([\w\d-_\.]+)\/?\??([^#\n\r]*)?#?([^\n\r]*)"
            }, 
                
            // regex taken from regex lib: http://regexlib.com/REDetails.aspx?regexp_id=247  
            {
                RecognitionType.Name,
                @"^([ \u00c0-\u01ffa-zA-Z'])+$"
            },

            {
                RecognitionType.PhoneNumber,
                @"(((\+[0-9]{1,2}|00[0-9]{1,2})[-\ .]?)?)(\d[-\ .]?){5,15}"
            },

            {
                RecognitionType.Number,
                @"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$"
            },
        };

        public static RecognitionType Recognize(string businessCardText)
        {
            RecognitionType type = RecognitionType.Other;
            //iterate through each type to try and find a match.
            // once a match is found stop and return the type
            foreach (KeyValuePair<RecognitionType, string> expression in expressions)
            {
                if (Regex.IsMatch(businessCardText, expression.Value))
                {
                    type = expression.Key;
                    break;
                }
            }

            return (type);
        }
    }
}