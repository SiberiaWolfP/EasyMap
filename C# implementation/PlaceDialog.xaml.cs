using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using static EasyMap.Common;

namespace EasyMap
{
    /// <summary>
    /// PlaceDialog.xaml 的交互逻辑
    /// </summary>
    public partial class PlaceDialog : Window
    {
        public PlaceDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            PlacePoint place = new PlacePoint(TextBoxName.Text, 
                Double.Parse(TextBoxLng.Text), 
                Double.Parse(TextBoxLat.Text));
            Places.Add(place);
            Map.AddPlaceSuccess(place.ToString());
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Map.AddPlaceFail();
        }
    }
}
