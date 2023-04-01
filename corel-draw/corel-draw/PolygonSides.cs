﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace corel_draw
{
    public partial class PolygonSides : Form
    {
        public int Sides { get; set; }
        public PolygonSides()
        {
            InitializeComponent(); 
        }

        private void PolygonSides_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(Polygon_Sides.Text, out int number))
            {
                if (number > 5 || number < 2)
                {
                    MessageBox.Show("Please enter a number between 2 and 5.");
                }
                else
                {
                    Sides = number;
                    Close();
                    PolygonTypeForm polygonTypeForm = new PolygonTypeForm(Sides);
                    polygonTypeForm.ShowDialog();
                }
            }
            else
                MessageBox.Show("Please enter a valid number.");
        }
    }
}