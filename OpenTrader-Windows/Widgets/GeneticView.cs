using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Data;

namespace OpenTrader
{
    public class GeneticView : GroupBox
    {
        private List<Phenotype> mGenePool;
        private List<StrategyParameter> mParameters;
        private DataGrid mTreeView;

        public List<Phenotype> GenePool
        {
            set
            {
                mGenePool = value;
                if (mTreeView != null)
                    mTreeView.ItemsSource = mGenePool;
            }
        }

        public GeneticView(List<StrategyParameter> Parameters) : base()
        {
            mParameters = Parameters;

            mTreeView = new DataGrid();
            mTreeView.Style = FindResource("ReadOnlyGridStyle") as Style;

            // Add a column for each parameter
            foreach (StrategyParameter Parameter in Parameters)
            {
                mTreeView.Columns.Add(new DataGridTextColumn()
                {
                    Header = Parameter.Name,
                    Width = new DataGridLength(200),
                    FontSize = 12,
                    Binding = new Binding("Name"),
                    CanUserResize = true
                });

                // parameterColumn.SetCellDataFunc (parameterCell, new Gtk.TreeCellDataFunc (RenderParameter));
            }

            // Create a column for the fitness
            mTreeView.Columns.Add(new DataGridTextColumn()
            {
                Header = "Fitness",
                Width = new DataGridLength(200),
                FontSize = 12,
                Binding = new Binding("Fitness"),
                CanUserResize = true
            });

            // fitnessColumn.SetCellDataFunc (fitnessCell, new Gtk.TreeCellDataFunc (RenderFitness));

            mTreeView.ItemsSource = mGenePool;
            ScrollViewer scrolledwindow = new ScrollViewer();
            scrolledwindow.Content = mTreeView;
            base.Content = scrolledwindow;
        }
    }
}

