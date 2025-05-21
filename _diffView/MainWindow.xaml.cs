using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace TextDiffViewer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // MVVM Fields
        private string _leftText = "";
        private string _rightText = "";
        private SideBySideDiffModel _diffModel;
        private bool _showLineNumbers = true;
        private int _contextLines = 3;

        // Colors for diff highlighting
        private readonly SolidColorBrush _addedBackground = new SolidColorBrush(Color.FromRgb(198, 239, 206));
        private readonly SolidColorBrush _deletedBackground = new SolidColorBrush(Color.FromRgb(255, 199, 206));
        private readonly SolidColorBrush _unchangedBackground = new SolidColorBrush(Colors.Transparent);
        private readonly SolidColorBrush _modifiedBackground = new SolidColorBrush(Color.FromRgb(255, 235, 156));

        // Constructor
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        // Properties with INotifyPropertyChanged
        public string LeftText
        {
            get => _leftText;
            set
            {
                if (_leftText != value)
                {
                    _leftText = value;
                    OnPropertyChanged();
                    UpdateDiff();
                }
            }
        }

        public string RightText
        {
            get => _rightText;
            set
            {
                if (_rightText != value)
                {
                    _rightText = value;
                    OnPropertyChanged();
                    UpdateDiff();
                }
            }
        }

        public bool ShowLineNumbers
        {
            get => _showLineNumbers;
            set
            {
                if (_showLineNumbers != value)
                {
                    _showLineNumbers = value;
                    OnPropertyChanged();
                    UpdateDiff();
                }
            }
        }

        public int ContextLines
        {
            get => _contextLines;
            set
            {
                if (_contextLines != value)
                {
                    _contextLines = value;
                    OnPropertyChanged();
                    UpdateDiff();
                }
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Load files
        private void LoadLeftFile_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ShowOpenFileDialog();
            if (!string.IsNullOrEmpty(filePath))
            {
                LeftText = File.ReadAllText(filePath);
            }
        }

        private void LoadRightFile_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ShowOpenFileDialog();
            if (!string.IsNullOrEmpty(filePath))
            {
                RightText = File.ReadAllText(filePath);
            }
        }

        private string ShowOpenFileDialog()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Select a text file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        // Compare button click
        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateDiff();
        }

        // Update diff and visualize
        private void UpdateDiff()
        {
            leftDiffPanel.Children.Clear();
            rightDiffPanel.Children.Clear();

            if (string.IsNullOrEmpty(LeftText) && string.IsNullOrEmpty(RightText))
            {
                return;
            }

            var diffBuilder = new SideBySideDiffBuilder(new Differ());
            _diffModel = diffBuilder.BuildDiffModel(LeftText, RightText, ignoreWhitespace: false);

            // Process and display the differences
            VisualizeDiff();
        }

        private void VisualizeDiff()
        {
            if (_diffModel == null) return;

            var leftLines = _diffModel.OldText.Lines;
            var rightLines = _diffModel.NewText.Lines;

            // Display left side (old text)
            for (int i = 0; i < leftLines.Count; i++)
            {
                var line = leftLines[i];
                var panel = new StackPanel { Orientation = Orientation.Horizontal };

                // Line number
                if (ShowLineNumbers)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = (i + 1).ToString(),
                        Width = 30,
                        Foreground = Brushes.Gray
                    });
                }

                // Line content with formatting
                var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
                FormatLine(textBlock, line, false);

                // Set background based on line type
                SolidColorBrush background = _unchangedBackground;
                if (line.Type == ChangeType.Deleted)
                {
                    background = _deletedBackground;
                }
                else if (line.Type == ChangeType.Modified)
                {
                    background = _modifiedBackground;
                }

                textBlock.Background = background;
                panel.Children.Add(textBlock);
                leftDiffPanel.Children.Add(panel);
            }

            // Display right side (new text)
            for (int i = 0; i < rightLines.Count; i++)
            {
                var line = rightLines[i];
                var panel = new StackPanel { Orientation = Orientation.Horizontal };

                // Line number
                if (ShowLineNumbers)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = (i + 1).ToString(),
                        Width = 30,
                        Foreground = Brushes.Gray
                    });
                }

                // Line content with formatting
                var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
                FormatLine(textBlock, line, true);

                // Set background based on line type
                SolidColorBrush background = _unchangedBackground;
                if (line.Type == ChangeType.Inserted)
                {
                    background = _addedBackground;
                }
                else if (line.Type == ChangeType.Modified)
                {
                    background = _modifiedBackground;
                }

                textBlock.Background = background;
                panel.Children.Add(textBlock);
                rightDiffPanel.Children.Add(panel);
            }
        }

        private void FormatLine(TextBlock textBlock, DiffPiece line, bool isNewText)
        {
            if (string.IsNullOrEmpty(line.Text))
            {
                textBlock.Inlines.Add(new Run(" "));
                return;
            }

            // Process character-level differences for modified lines
            if (line.Type == ChangeType.Modified && line.SubPieces != null)
            {
                foreach (var piece in line.SubPieces)
                {
                    var run = new Run(piece.Text);

                    if ((isNewText && piece.Type == ChangeType.Inserted) ||
                        (!isNewText && piece.Type == ChangeType.Deleted))
                    {
                        run.Background = isNewText ? _addedBackground : _deletedBackground;
                        run.FontWeight = FontWeights.Bold;
                    }
                    else if (piece.Type == ChangeType.Unchanged)
                    {
                        // Keep unchanged text as is
                    }

                    textBlock.Inlines.Add(run);
                }
            }
            else
            {
                // For non-modified lines or lines without character diffs
                var run = new Run(line.Text);

                if (line.Type == ChangeType.Deleted && !isNewText)
                {
                    run.TextDecorations = TextDecorations.Strikethrough;
                }
                else if (line.Type == ChangeType.Inserted && isNewText)
                {
                    run.FontWeight = FontWeights.Bold;
                }

                textBlock.Inlines.Add(run);
            }
        }
    }
}