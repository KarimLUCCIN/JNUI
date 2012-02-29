using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KinectBrowser.T9Keyboard;

namespace KinectBrowser
{
	/// <summary>
	/// Interaction logic for KeyboardControl.xaml
	/// </summary>
	public partial class KeyboardT9 : UserControl
	{
		/// <summary>
        /// MVVM ViewModel.
        /// </summary>
        private T9Keyboard.T9ViewModel viewModel;
		public string inputUserCode;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public KeyboardT9()
        {
            InitializeComponent();

            this.viewModel = new T9ViewModel();
            this.viewModel.MinimumAutoCompleteTreshold = 3;
            this.viewModel.MaximumAutoCompleteTreshold = 10;
            this.DataContext = this.viewModel;
			this.viewModel.IsAutoComplete = true;
        }

        /// <summary>
        /// Adds a digit.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void DigitButton_Click(object sender, RoutedEventArgs e)
        {
            this.inputUserCode += (sender as Control).TabIndex;
			this.viewModel.EncodedText = this.inputUserCode;
			
			/*if (this.CandidatesListBox.Items.Count == 0 )
            {*/
                this.inputUser.Text += (sender as Control).Tag;
            /*}
			else 
			{
				this.inputUser.Text = this.CandidatesListBox.Items[0].ToString();
				this.inputUserCode =  this.viewModel.EncodeString(this.CandidatesListBox.Items[0].ToString());
			}*/
			
			if (this.CandidatesListBox.Items.Count == 1)
			{
				this.inputUser.Text = this.CandidatesListBox.Items[0].ToString();
				this.inputUserCode =  this.viewModel.EncodeString(this.CandidatesListBox.Items[0].ToString());
			}
        }

        /// <summary>
        /// Selects a candidate.
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void CandidatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.CandidatesListBox.SelectedIndex < 0)
            {
                return;
            }

            //this.inputUser.Text = this.viewModel.EncodeString(this.CandidatesListBox.SelectedValue.ToString());
        	this.inputUser.Text = this.CandidatesListBox.SelectedValue.ToString();
			this.inputUserCode = this.viewModel.EncodeString(this.CandidatesListBox.SelectedValue.ToString());
		}
		
		
        private void backButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
			updateCandidates();
		}
		
		private void updateCandidates()
		{
			if(this.inputUserCode.Length > 0) 
			{
				this.inputUserCode = this.inputUserCode.Substring(0, this.inputUserCode.Length -1);
				this.viewModel.EncodedText = this.inputUserCode;
			
				if(inputUserCode.Length < 3 && this.CandidatesListBox.Items.Count > 0)
				{
					this.viewModel.clearCandidates();
				}
			}
		}
		
		private void Keyboard_initialized(object sender, System.EventArgs e)
		{	
			/*KeyboardButtons abc1 = new KeyboardButtons("abc1");
			this.buttonGrid.Children.Add(abc1);
			
			KeyboardButtons def2 = new KeyboardButtons("def2");
			this.buttonGrid.Children.Add(def2);
			
			KeyboardButtons ghi3 = new KeyboardButtons("ghi3");
			this.buttonGrid.Children.Add(ghi3);
			
			KeyboardButtons jkl4 = new KeyboardButtons("jkl4");
			this.buttonGrid.Children.Add(jkl4);
			
			KeyboardButtons mno5 = new KeyboardButtons("mno5");
			this.buttonGrid.Children.Add(mno5);
			
			KeyboardButtons pqr6 = new KeyboardButtons("pqr6");
			this.buttonGrid.Children.Add(pqr6);
			
			KeyboardButtons stu7 = new KeyboardButtons("stu7");
			this.buttonGrid.Children.Add(stu7);
			
			KeyboardButtons vwx8 = new KeyboardButtons("vwx8");
			this.buttonGrid.Children.Add(vwx8);
			
			KeyboardButtons yzat9 = new KeyboardButtons("yz@9");
			this.buttonGrid.Children.Add(yzat9);
			
			KeyboardButtons symb = new KeyboardButtons("./0");
			this.buttonGrid.Children.Add(symb);*/
		}

		private void inputUser_Initialized(object sender, System.EventArgs e)
		{
			//inputUser.SpellCheck.IsEnabled = true;
			//inputUser.SpellCheck.SpellingReform = SpellingReform.PreAndPostreform;
		}

		private void inputUser_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
            //UpdateSuggestions();
		}

        public void UpdateSuggestions()
        {
            /*int caretPos = inputUser.CaretIndex;
            
            listBox.Items.Clear();

            if (!String.IsNullOrEmpty(inputUser.Text))
            {
                error = inputUser.GetSpellingError(0);
                if (error != null)
                {
                    var corrections = from sug in error.Suggestions orderby LevenshteinDistance.Compute(inputUser.Text, sug) select sug;
                    int loc = 0;
                    var limited_corrections = corrections.TakeWhile(str => { if (loc >= 4) return false; loc++; return true; });

                    foreach (string suggession in limited_corrections)
                    {
                        listBox.Items.Add(suggession);
                    }
                }
            }*/
        }
		
		private void correctSelected(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//error.Correct(listBox.SelectedItem.ToString());
		}

        public void ClearSuggestions()
        {
            //listBox.Items.Clear();
        }
    }
}