namespace KinectBrowser.T9Keyboard
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;

    /// <summary>
    /// ViewModel States.
    /// </summary>
    public enum States
    {
        /// <summary>
        /// Dictionary is not searched.
        /// </summary>
        Neutral,

        /// <summary>
        /// Only one word in the dictionary matches the T9 string.
        /// </summary>
        SingleMatch,

        /// <summary>
        /// Multiple words in the dictionary matches the T9 string.
        /// </summary>
        MultipleMatches,

        /// No words in the dictionary matches the T9 string.
        NoMatch
    }

    /// <summary>
    /// T9 MVVM ViewModel.
    /// </summary>
    public class T9ViewModel : INotifyPropertyChanged
    {
        private string encodedText;

        private bool isAutoComplete;

        private int minimumAutoCompleteTreshold;

        private int maximumAutoCompleteTreshold;

        private T9Encoder encoder;

        private States state;

        /// <summary>
        /// List of known words.
        /// </summary>
        private List<KeyValuePair<string, string>> dictionary = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// List of candidate words in autocomplete mode.
        /// </summary>
        private List<string> candidates;

        /// <summary>
        /// Initializes a new instance of the T9ViewModel class.
        /// </summary>
        public T9ViewModel()
        {
            this.encoder = new T9Encoder();

            string[] words = File.ReadAllLines("../KinectBrowser/english-words");
			
			foreach (string s in words) 
			{
				this.AddWord(s);
			}

            this.encoder.Dictionary = this.dictionary;

            this.state = States.Neutral;
        }

        /// <summary>
        /// Notifies property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        public States State
        {
            get
            {
                return this.state;
            }

            set
            {
                this.state = value;
                this.PropertyChanged.Raise(this, o => o.State);
            }
        }

        /// <summary>
        /// Gets or sets the T9 encoded text.
        /// </summary>
        public string EncodedText
        {
            get
            {
                return this.encodedText;
            }

            set
            {
                this.encodedText = value;

                if (this.EncodedText.Length == this.maximumAutoCompleteTreshold)
                {
                    this.Candidates = this.encoder.DecodeString(this.encodedText);
                }
                else if (this.isAutoComplete && this.encodedText.Length >= this.minimumAutoCompleteTreshold)
                {
                    this.Candidates = this.encoder.Predict(this.encodedText);
                }
                else
                {
                    this.State = States.Neutral;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether AutoComplete mode is on.
        /// </summary>
        public bool IsAutoComplete
        {
            get
            {
                return this.isAutoComplete;
            }

            set
            {
                this.isAutoComplete = value;
            }
        }

        /// <summary>
        /// Gets or sets the list of candidate words for a T9-encoded prefix.
        /// </summary>
        public List<string> Candidates
        {
            get
            {
                return this.candidates;
            }

            set
            {
                this.candidates = value;
                this.PropertyChanged.Raise(this, o => o.Candidates);
                this.UpdateState();
            }
        }

        /// <summary>
        /// Gets or sets the minimum length for triggering autocompletion.
        /// </summary>
        public int MinimumAutoCompleteTreshold
        {
            get
            {
                return this.minimumAutoCompleteTreshold;
            }

            set
            {
                this.minimumAutoCompleteTreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum length for triggering autocompletion.
        /// </summary>
        public int MaximumAutoCompleteTreshold
        {
            get
            {
                return this.maximumAutoCompleteTreshold;
            }

            set
            {
                this.maximumAutoCompleteTreshold = value;
            }
        }

        /// <summary>
        /// Removes the diacritics from a string.
        /// </summary>
        internal string RemoveDiacritics(string clearText)
        {
            return this.encoder.RemoveDiacritics(clearText);
        }

        /// <summary>
        /// Encodes a string to the T9 format.
        /// </summary>
        internal string EncodeString(string clearText)
        {
            return this.encoder.EncodeString(clearText);
        }

        /// <summary>
        /// Adds a word to the T9 dictionary.
        /// </summary>
        private void AddWord(string word)
        {
            this.dictionary.Add(new KeyValuePair<string, string>(this.encoder.EncodeString(word), word));
        }

        /// <summary>
        /// Updates the state.
        /// </summary>
        private void UpdateState()
        {
            if (this.Candidates.Count == 0)
            {
                this.State = States.NoMatch;
            }
            else if (this.Candidates.Count == 1)
            {
                this.State = States.SingleMatch;
            }
            else
            {
                this.State = States.MultipleMatches;
            }
        }
    }
}
