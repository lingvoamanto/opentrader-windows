using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenTrader.Windows
{
    /// <summary>
    /// Interaction logic for JournalWindow.xaml
    /// </summary>
    /// 

    public partial class JournalWindow : Window
    {
        Data.DataFile? dataFile;
        List<Data.JournalEntry> journalEntries;
        Data.JournalEntry? journalEntry;
        public Data.DataFile DataFile
        {
            get => dataFile;

            set
            {
                dataFile = value;
                UpdateElements();
            }

        }
        public JournalWindow()
        {
            InitializeComponent();
        }

        private void UpdateElements()
        {
            ListBox.Items.Clear();
            if (dataFile != null)
            {
                journalEntries = Data.JournalEntry.GetYahooCode(dataFile.YahooCode);

                foreach (var journalEntry in journalEntries)
                {
                    var label = new Label()
                    {
                        Content = journalEntry.Date.ToShortDateString(),
                        Tag = journalEntry
                    };
                    ListBox.Items.Add(label);
                }

                Description.Text = dataFile.Description;

                if(journalEntries.Count == 0)
                {
                    Editor.Text = "";
                }
                else
                {
                    var item = ListBox.Items[0] as Label;
                    ListBox.SelectedItem = item;
                    if (item != null && item.Tag is Data.JournalEntry journalEntry)
                    {
                        this.journalEntry = journalEntry;
                        Editor.Text = journalEntry.Notes;
                        DatePicker.SelectedDate = journalEntry.Date;
                    }
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataFile == null)
                return;

            var journalEntry = new Data.JournalEntry()
            {
                Date = DateTime.Now,
                Notes = "",
                YahooCode = dataFile.YahooCode
            };
            this.journalEntry = journalEntry;

            var label = new Label()
            {
                Content = journalEntry.Date.ToShortDateString(),
                Tag = journalEntry
            };
            ListBox.Items.Insert(0, label);
            ListBox.SelectedItem = label;

            DatePicker.SelectedDate = journalEntry.Date;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            journalEntry?.Save();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            journalEntry?.Remove();
            var item = ListBox.SelectedItem as Label;
            ListBox.Items.Remove(item);
        }

        private void SelectedDateChanged(object sender, RoutedEventArgs e)
        {
            if( DatePicker.SelectedDate != null && journalEntry != null )
                journalEntry.Date = DatePicker.SelectedDate.Value;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ListBox.SelectedItem as Label;
            if (item != null && item.Tag is Data.JournalEntry journalEntry)
            {
                this.journalEntry = journalEntry;
                Editor.Text = journalEntry.Notes;
                DatePicker.SelectedDate = journalEntry.Date;
            }
        }
    }
}
