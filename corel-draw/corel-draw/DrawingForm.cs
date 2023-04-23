﻿using corel_draw.Components;
using corel_draw.FactoryComponents;
using corel_draw.Figures;
using CorelLibary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Button = System.Windows.Forms.Button;

namespace corel_draw
{
    public partial class DrawingForm : Form
    {
        private readonly List<Figure> _drawnFigures;
        private readonly IReadOnlyList<FigureFactory> _figureFactories;
        private readonly CommandManager _commandManager;
        private readonly Dictionary<string, Figure> _specialTags = new Dictionary<string, Figure>();

        private FigureFactory _figureFactory;
        private Figure _currentFigure;
        private Action<Figure> _figureFinishedHandler;
        
        private Point _lastPoint = Point.Empty;
        private Point _initialPosition = Point.Empty;

        private bool _isDragging = false;
        private bool _isFilling = false;

        private const string PATH = "../../JsonFiles/DataFigures.json";

        public DrawingForm(IReadOnlyList<FigureFactory> figureFactories)
        {
            InitializeComponent(); 
            _commandManager = new CommandManager();
            _drawnFigures = new List<Figure>();
            this.KeyPreview = true;
            _figureFactories = figureFactories;
        }

        private void DrawingForm_Load(object sender, EventArgs e)
        {
            Type[] figureTypes = typeof(Figure).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Figure))).ToArray();
            int buttonWidth = (Width - 150) / figureTypes.Length;
            for (int i = 0; i < _figureFactories.Count; i++)
            {
                Type figureType = figureTypes[i];
                int index = i;
                Button button = new Button
                {
                    Text = figureType.Name,
                    Tag = figureType,
                    Height = 75,
                    Width = buttonWidth,
                    Left = i * buttonWidth + 75,
                    Top = Height - 150
                };

                button.Click += (object sender1, EventArgs e1) =>
                {
                    _figureFactory = _figureFactories[index];
                    _figureFactory.BeginCreateFigure();
                    _isFilling = false;
                };
                Controls.Add(button);

                
                _figureFinishedHandler = (figure) =>
                {
                    ICommand addCommand = new AddCommand(figure, _drawnFigures);
                    _commandManager.AddCommand(addCommand);
                    _figureFactory = null;
                    actionList.Items.Add($"Added {figure.GetType().Name} with area of {figure.CalcArea():F2}");
                    DrawingBox.Invalidate();
                }; 
                _figureFactories[index].Finished += _figureFinishedHandler;
            }
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentFigure != null)
            {
                ICommand removeCommand = new DeleteCommand(_currentFigure, _drawnFigures);
                _commandManager.AddCommand(removeCommand);
                actionList.Items.Add($"Delete {_currentFigure.GetType().Name}");
                _currentFigure = null;
                DrawingBox.Invalidate();
                return;
            }
        }

        private void ColorMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                if (_currentFigure != null)
                {
                    Color oldColor = _currentFigure.Color;
                    Color newColor = colorDialog.Color;

                    ICommand colorCommand = new ColorCommand(_currentFigure, oldColor, newColor);
                    _commandManager.AddCommand(colorCommand);

                    actionList.Items.Add($"Change {_currentFigure.GetType().Name} Color with {_currentFigure.Color.Name}");
                    _currentFigure.Color = newColor;
                    DrawingBox.Invalidate(); 
                    return;
                }
            }
        }

        private void EditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Figure oldState = _currentFigure;
            int matchingIndex = -1;
            Type[] figureTypes = typeof(Figure).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Figure))).ToArray();

            for (int i = 0; i < figureTypes.Length; i++)
            {
                if (_currentFigure.GetType() == figureTypes[i])
                {
                    matchingIndex = i;
                    break;
                }
            }
            _figureFactory = _figureFactories[matchingIndex];
            _figureFactory.BeginCreateFigure();

            _figureFactories[matchingIndex].Finished -= _figureFinishedHandler;
            _figureFactories[matchingIndex].Finished += (figure) =>
            {
                ICommand command = new EditCommand(oldState, figure);
                _commandManager.AddCommand(command);
                _figureFactory = null;
                _currentFigure = figure;
                actionList.Items.Add($"Edit {oldState.GetType().Name} with new area of {figure.CalcArea():F2}");
                DrawingBox.Invalidate();
            };
        }

        private void FillMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                if (_currentFigure != null)
                {
                    Color oldFilling = _currentFigure.FillColor;
                    Color newFilling = colorDialog.Color;
                    _isFilling = true;

                    ICommand command = new FillCommand(_currentFigure, oldFilling, newFilling);
                    _commandManager.AddCommand(command);

                    actionList.Items.Add($"Change {_currentFigure.GetType().Name} Fill Color with {_currentFigure.FillColor.Name}");
                    _currentFigure.FillColor = newFilling;
                    DrawingBox.Invalidate();
                    return;
                }
            }
        }

        private void AdditionalInfoMenuItem_Click(object sender, EventArgs e)
        {
            Figure largestFigure = _drawnFigures.OrderByDescending(f => f.CalcArea()).First();
            Figure smallestFigure = _drawnFigures.OrderBy(f => f.CalcArea()).First();
            Figure firstFigure = _drawnFigures.First();
            Figure lastFigure = _drawnFigures.Last();

            if(_drawnFigures.Any(type => type is Polygon))
            {
                int maxSides = _drawnFigures.OfType<Polygon>().Select(polygon => polygon.Points.Count).Max();
                Figure polygonMostSides = _drawnFigures.FirstOrDefault(polygon => polygon is Polygon polygon1 && polygon1.Points.Count == maxSides);
                _specialTags.Add("Polygon with Most Sides", polygonMostSides);
            }

            _specialTags.Add("Biggest Figure by Area",largestFigure);
            _specialTags.Add("Smallest Figure by Area",smallestFigure);
            _specialTags.Add("First Created Figure", firstFigure);
            _specialTags.Add("Last Created Figure", lastFigure);
            MessageBox.Show(_specialTags.Count.ToString());
            AdditionalInfo additionalInfo = new AdditionalInfo(_specialTags);
            additionalInfo.ShowDialog();
        }

        private void DrawingBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (_figureFactory != null)
            {
                _figureFactory.MouseDown(e);
                DrawingBox.Refresh();
                return;
            }
            foreach (Figure figure in _drawnFigures)
            {
                if (figure.Contains(e.Location))
                {
                    _currentFigure = figure;
                    if (e.Button == MouseButtons.Left)
                    {
                        _isDragging = true;
                        _lastPoint = e.Location;
                        _initialPosition = figure.Location;
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();

                        contextMenuStrip.Items.Add("Delete").Click += DeleteMenuItem_Click;
                        contextMenuStrip.Items.Add("Change Border Color").Click += ColorMenuItem_Click;
                        contextMenuStrip.Items.Add("Fill Figure").Click += FillMenuItem_Click;
                        contextMenuStrip.Items.Add("Edit").Click += EditToolStripMenuItem_Click;
                        contextMenuStrip.Items.Add("Info").Click += AdditionalInfoMenuItem_Click;
                        contextMenuStrip.Items[2].Tag = _currentFigure;

                        contextMenuStrip.Show(DrawingBox, e.Location);
                    }
                    break;
                }
            }
        }

        private void DrawingBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point delta = new Point(e.X - _lastPoint.X, e.Y - _lastPoint.Y);
                _lastPoint = e.Location;
                _currentFigure.Move(delta);
                DrawingBox.Invalidate();
                return;
            }
            
            if( _figureFactory != null)
            {
                _figureFactory.MouseMove(e);
                DrawingBox.Refresh();   
            }
        }

        private void DrawingBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                ICommand moveCommand = new MoveCommand(_currentFigure, _currentFigure.Location);
                _commandManager.AddCommand(moveCommand);
                actionList.Items.Add($"Move {_currentFigure.GetType().Name}");
                _isDragging = false;
                _lastPoint = Point.Empty;
                return;
            }

            if (_figureFactory != null)
            {
                _figureFactory.MouseUp(e);
                DrawingBox.Refresh();
            }
        }

        private void DrawingBox_Paint(object sender, PaintEventArgs e)
        {
            _figureFactory?.Draw(e.Graphics); 
            foreach (Figure figure in _drawnFigures)
            {
                figure.Draw(e.Graphics);
                if(_isFilling)
                {
                    figure.Fill(e.Graphics);
                }
            }
        }

        private void Redo_Btn_Click(object sender, EventArgs e)
        {
            if (_commandManager.CanRedo)
            {
                _commandManager.Redo();
                DrawingBox.Invalidate();
            }
        }

        private void Undo_Btn_Click(object sender, EventArgs e)
        {
            if (_commandManager.CanUndo)
            {
                _commandManager.Undo();
                DrawingBox.Invalidate();
            }
        }

        private void SaveToFile_Click(object sender, EventArgs e)
        {
            try
            {
                DrawingData drawingData = new DrawingData { DrawnFigures = _drawnFigures.ToList() };
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };
                string json = JsonConvert.SerializeObject(drawingData, Formatting.Indented, settings);
                File.WriteAllText(PATH, json);
                MessageBox.Show("File saved successfully.");
                actionList.Items.Add("Save figures to file");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}");
            }
        }

        private void LoadFromFile_Click(object sender, EventArgs e)
        {
            try
            {
                string json = File.ReadAllText(PATH);
                DrawingData drawingData = JsonConvert.DeserializeObject<DrawingData>(json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

                List<Figure> loadedFigures = new List<Figure>();
                loadedFigures.AddRange(drawingData.DrawnFigures);

                ICommand loadCommand = new LoadCommand(_drawnFigures, loadedFigures);
                _commandManager.AddCommand(loadCommand);

                DrawingBox.Invalidate();
                MessageBox.Show("File loaded successfully.");
                actionList.Items.Add("Load figures from file");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}");
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Z when e.Control:
                    if (_commandManager.CanUndo)
                    {
                        _commandManager.Undo();
                        DrawingBox.Invalidate();
                    }
                    break;

                case Keys.Y when e.Control:
                    if (_commandManager.CanRedo)
                    {
                        _commandManager.Redo();
                        DrawingBox.Invalidate();
                    }
                    break;

                case Keys.Escape:
                    _figureFactory = null;
                    DrawingBox.Refresh();
                    break;
            }
        }
    }
}
