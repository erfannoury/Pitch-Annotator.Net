using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;

namespace PitchAnnotator
{
    public class LineListItemConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts source values to a value for the binding target
        /// </summary>
        /// <param name="values"> {X1, X2, Y1, Y2} </param>
        /// <returns>Proper LineEntry label text that will be updated</returns>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length != 4)
                return "Binding is incorrect";
            string labelText;
            try
            {
                labelText = string.Format("Line ({0},{1}) - ({2},{3})",
                        ((double)values[0]).ToString("F3"), ((double)values[2]).ToString("F3"),
                        ((double)values[1]).ToString("F3"), ((double)values[3]).ToString("F3"));
            }
            finally
            {
                labelText = "";
            }
            return labelText;
        }

        /// <summary>
        /// Converts a binding target value to the source binding values
        /// Since no back-convesions are needed, this method will not be implemented
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Class for creating appropriate ListItems for the ListView showing line layers for each image
    /// It has the full functionality of the Label class, but also contains a reference to a Line object
    /// </summary>
    class LineEntry : Label
    {
        /// <summary>
        /// This is set to true, when the corresponding LineEntry is selected (so that when the item is selected, mouse_leave event be disabled)
        /// </summary>
        public bool IsItemSelected;

        /// <summary>
        /// This is a reference to the corresponding line item
        /// </summary>
        public Line LineReference;

        /// <summary>
        /// This multibinding will cause the label of the linelistitem get updated as the corresponding line endpoints move
        /// </summary>
        private MultiBinding LineLabelMultiBinding;

        public LineEntry(Line lineref)
        {
            this.LineReference = lineref;

            // Multibinding to update label
            LineLabelMultiBinding = new MultiBinding();
            LineLabelMultiBinding.Converter = new LineListItemConverter();
            LineLabelMultiBinding.Bindings.Add(new Binding("X1") { Source = this.LineReference });
            LineLabelMultiBinding.Bindings.Add(new Binding("X2") { Source = this.LineReference });
            LineLabelMultiBinding.Bindings.Add(new Binding("Y1") { Source = this.LineReference });
            LineLabelMultiBinding.Bindings.Add(new Binding("Y2") { Source = this.LineReference });
            this.SetBinding(ContentProperty, LineLabelMultiBinding);


            this.Background = Brushes.White;
            this.Width = 270;


            this.MouseEnter += LineListItem_MouseEnter;
            this.MouseLeave += LineListItem_MouseLeave;

            this.IsItemSelected = false;
        }

        /// <summary>
        /// Event raised when mouse enteres this element
        /// </summary>
        void LineListItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if(!this.IsItemSelected)
            {
                this.LineReference.Stroke = Brushes.Red;
            }
        }
        /// <summary>
        /// Event raised when mouse leaves this element
        /// </summary>
        void LineListItem_MouseEnter(object sender, MouseEventArgs e)
        {
            this.LineReference.Stroke = Brushes.Orange;
        }
    }
}
