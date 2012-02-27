namespace KinectBrowser.T9Keyboard
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Encodes to the T9 format.
    /// </summary>
    public class T9Encoder
    {
        private List<KeyValuePair<string, string>> dictionary = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// Gets or sets the words dictionary.
        /// </summary>
        public List<KeyValuePair<string, string>> Dictionary
        {
            get
            {
                return this.dictionary;
            }

            set
            {
                this.dictionary = value;
            }
        }

        /// <summary>
        /// Encodes a string to the T9 format.
        /// </summary>
        public string EncodeString(string clearText)
        {
            // Normalize and convert to lowercase.
            string result = this.RemoveDiacritics(clearText).ToLower();

            // Remove digits.
            result = Regex.Replace(result, "[2-9]", string.Empty);

            // Translate to SMS.
            result = Regex.Replace(result, "[abc]", "2");
            result = Regex.Replace(result, "[def]", "3");
            result = Regex.Replace(result, "[ghi]", "4");
            result = Regex.Replace(result, "[jkl]", "5");
            result = Regex.Replace(result, "[mno]", "6");
            result = Regex.Replace(result, "[pqrs]", "7");
            result = Regex.Replace(result, "[tuv]", "8");
            result = Regex.Replace(result, "[wxyz]", "9");

            // Replace remaining non-SMS characters by word boundary.
            result = Regex.Replace(result, "[^2-9]", " ");

            return result;
        }

        /// <summary>
        /// Decodes a T9 string to a word in the dictionary.
        /// </summary>
        public List<string> DecodeString(string t9Text)
        {
            return (from w in this.dictionary
                    where w.Key == t9Text
                    select w.Value).ToList();
        }

        /// <summary>
        /// Predicts a T9 decoding, based on a prefix.
        /// </summary>
        public List<string> Predict(string prefix)
        {
            return (from w in this.dictionary
                    where w.Key.StartsWith(prefix)
                    select w.Value).ToList();
        }

        /// <summary>
        /// Normalizes a string and removes diacritics.
        /// </summary>
        public string RemoveDiacritics(string clearText)
        {
            string normalizedText = clearText.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();
            foreach (char ch in normalizedText)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
