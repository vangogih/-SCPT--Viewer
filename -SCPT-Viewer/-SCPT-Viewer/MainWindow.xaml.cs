using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SCPT.Helper;
using SCPT.Transformation;
using Point = SCPT.Helper.Point;

namespace _SCPT_Viewer
{
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
        private List<Point> _sourcePoints;
        private List<Point> _destinationPoints;


        public MainWindow()
        {
            InitializeComponent();

            SourceGrid.CanUserResizeColumns = false;
            SourceGrid.FrozenColumnCount = 4;

            SeparatorSourceComboBox.SelectedIndex = 0;
            separatorDestinationComboBox.SelectedIndex = 0;

            _sourcePoints = new List<Point>();
            _destinationPoints = new List<Point>();
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

            try
            {
                foreach (var row in data)
                {
                    if (isSource)
                    {
                        var col = row.Split((char) _sourceSeparator);

                        if (col.Length == 3)
                            _sourcePoints.Add(new Point(Convert.ToDouble(col[0]), Convert.ToDouble(col[1]),
                                Convert.ToDouble(col[2])));
                        else
                            _sourcePoints.Add(new Point(Convert.ToDouble(col[1]), Convert.ToDouble(col[2]),
                                Convert.ToDouble(col[3]), col[0]));
                    }
                    else
                    {
                        var col = row.Split((char) _destinationSeparator);
                        if (col.Length == 3)
                            _destinationPoints.Add(new Point(Convert.ToDouble(col[0]), Convert.ToDouble(col[1]),
                                Convert.ToDouble(col[2])));
                        else
                            _destinationPoints.Add(new Point(Convert.ToDouble(col[1]), Convert.ToDouble(col[2]),
                                Convert.ToDouble(col[3]), col[0]));
                    }
                }

                if (isSource)
                    SourceGrid.ItemsSource = _sourcePoints;
                else
                    DestinationGrid.ItemsSource = _destinationPoints;
            }
            catch (Exception)
            {
                MessageBox.Show("Проверьте что вы правильно указали разделитель и что конечный файл не занят другой программой", "Ошибка чтения файла", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateParameters(object sender, RoutedEventArgs e)
        {
            if (_sourcePoints.Count == 0)
            {
                MessageBox.Show("Список исходных координат не может быть меньше 0", "Ошибка выполнения", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_destinationPoints.Count == 0)
            {
                MessageBox.Show("Список конечных координат не может быть меньше 0", "Ошибка выполнения", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var srcSc = new SystemCoordinate(_sourcePoints);
            var dstSc = new SystemCoordinate(_destinationPoints);
 
            var nip = new NewtonIterationProcess(srcSc, dstSc);
            NipParams.ItemsSource = FillParamsList(nip);
            var a9 = new NineAffine(srcSc, dstSc);
            A9Params.ItemsSource = FillParamsList(a9);
            var lp = new LinearProcedure(srcSc, dstSc);
            LpParams.ItemsSource = FillParamsList(lp);
            var svd = new SingularValueDecomposition(srcSc, dstSc);
            SvdParams.ItemsSource = FillParamsList(svd);

            var nipHelmert =
                new Helmert(srcSc).FromSourceToDestinationByParameters(nip.DeltaCoordinateMatrix,
                    nip.RotationMatrix, nip.M).GetSubtractList(dstSc);
            NipDataGrid.ItemsSource = RoundList(nipHelmert);

            var a9Helmert =
                new Helmert(srcSc).FromSourceToDestinationByParameters(a9.DeltaCoordinateMatrix,
                    a9.RotationMatrix, a9.M).GetSubtractList(dstSc);
            A9DataGrid.ItemsSource = RoundList(a9Helmert);

            var lpHelmert =
                new Helmert(srcSc).FromSourceToDestinationByParameters(lp.DeltaCoordinateMatrix,
                    lp.RotationMatrix, lp.M).GetSubtractList(dstSc);
            LinearDataGrid.ItemsSource = RoundList(lpHelmert);

            var svdHelmert =
                new Helmert(srcSc).FromSourceToDestinationByParameters(svd.DeltaCoordinateMatrix,
                    svd.RotationMatrix, svd.M).GetSubtractList(dstSc);

            SvdDataGrid.ItemsSource = RoundList(svdHelmert);
        }

        private List<ParamsString> FillParamsList(AbstractTransformation transformation)
        {
            var resultList = new List<ParamsString>
            {
                new ParamsString("dX, м", transformation.DeltaCoordinateMatrix.Dx.ToString("F5")),
                new ParamsString("dY, м", transformation.DeltaCoordinateMatrix.Dy.ToString("F5")),
                new ParamsString("dZ, м", transformation.DeltaCoordinateMatrix.Dz.ToString("F5")),
                new ParamsString("wX, rad", transformation.RotationMatrix.Wx.ToString("F15")),
                new ParamsString("wY, rad", transformation.RotationMatrix.Wy.ToString("F15")),
                new ParamsString("wZ, rad", transformation.RotationMatrix.Wz.ToString("F15")),
                new ParamsString("M, [-]", transformation.M.ToString("F15"))
            };
            return resultList;
        }

        private List<PointString> RoundList(List<Point> list)
        {
            var roundList = new List<PointString>();
            foreach (var point in list)
                roundList.Add(new PointString("", Math.Round(point.X, 4).ToString("F4"),
                    Math.Round(point.Y, 4).ToString("F4"), Math.Round(point.Z, 4).ToString("F4")));
            return roundList;
        }

        private class ParamsString
        {
            public string Name { get; }
            public string Value { get; }

            public ParamsString(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }

        private class PointString
        {
            public string Name { get; }
            public string X { get; }
            public string Y { get; }
            public string Z { get; }

            public PointString(string name, string x, string y, string z)
            {
                Name = name;
                X = x;
                Y = y;
                Z = z;
            }
        }
    }
}