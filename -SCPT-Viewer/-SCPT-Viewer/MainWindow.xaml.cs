using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace _SCPT_Viewer
{
    public class Points
    {
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Points(string name, double x, double y, double z)
        {
            Name = name;
            X = x;
            Y = y;
            Z = z;
        }
    }

    public partial class MainWindow : Window
    {
        private enum Separator
        {
            Semicolon = ';',
            Colon = ':',
            Comma = ',',
            Dot = '.',
            Space = ' '
        }

        private Separator _sourceSeparator;
        private Separator _destinationSeparator;
        private List<Points> _sourcePoints;
        private List<Points> _destinationPoints;

        public MainWindow()
        {
            InitializeComponent();

            SourceGrid.CanUserResizeColumns = false;
            SourceGrid.FrozenColumnCount = 4;

            SeparatorSourceComboBox.SelectedIndex = 0;
            separatorDestinationComboBox.SelectedIndex = 0;

            _sourcePoints = new List<Points>();
            _destinationPoints = new List<Points>();
        }

        private void ComboBox_Selected(object sender, RoutedEventArgs e)
        {
            var comboBox = (ComboBox) sender;

            var selectedItem = comboBox.SelectedIndex;
            var localSeparator = Separator.Semicolon;
            switch (selectedItem)
            {
                case 0:
                    localSeparator = Separator.Semicolon;
                    break;
                case 1:
                    localSeparator = Separator.Colon;
                    break;
                case 2:
                    localSeparator = Separator.Comma;
                    break;
                case 3:
                    localSeparator = Separator.Dot;
                    break;
                case 4:
                    localSeparator = Separator.Space;
                    break;
            }

            if (comboBox.Name == SeparatorSourceComboBox.Name)
                _sourceSeparator = localSeparator;
            else
                _destinationSeparator = localSeparator;
        }

        private void LoadingCoordinates(object sender, RoutedEventArgs e)
        {
            var button = (Button) sender;
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = false,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)
            };

            if (openFileDialog.ShowDialog() == true)
                ReadFile(openFileDialog.FileName, button.Name == LoadSourceCoordinatesButton.Name);
        }

        private void ReadFile(string path, bool isSource)
        {
            var data = File.ReadAllLines(path);

            foreach (var row in data)
            {
                if (isSource)
                {
                    var col = row.Split((char) _sourceSeparator);
                    _sourcePoints.Add(new Points("", Convert.ToDouble(col[0]), Convert.ToDouble(col[1]),
                        Convert.ToDouble(col[2])));
                }
                else
                {
                    var col = row.Split((char) _destinationSeparator);
                    _destinationPoints.Add(new Points("", Convert.ToDouble(col[0]), Convert.ToDouble(col[1]),
                        Convert.ToDouble(col[2])));
                }
            }

            if (isSource)
                SourceGrid.ItemsSource = _sourcePoints;
            else
                DestinationGrid.ItemsSource = _destinationPoints;
        }
    }
}