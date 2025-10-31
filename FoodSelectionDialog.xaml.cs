using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using menza_admin.Models;

namespace menza_admin
{
    public partial class FoodSelectionDialog : Window
    {
        private List<Food> _allFoods;
        private List<Food> _excludedFoods;

        public Food SelectedFood { get; private set; }

        public FoodSelectionDialog(List<Food> allFoods, List<Food> excludedFoods)
        {
            InitializeComponent();
            _allFoods = allFoods;
            _excludedFoods = excludedFoods;
            LoadFoods();
        }

        private void LoadFoods(string searchText = "")
        {
            var foods = _allFoods
                .Where(f => !_excludedFoods.Any(ef => ef.Id == f.Id))
                .Where(f => string.IsNullOrEmpty(searchText) || 
                           f.Name.ToLower().Contains(searchText.ToLower()) ||
                           (f.Description != null && f.Description.ToLower().Contains(searchText.ToLower())))
                .OrderBy(f => f.Name)
                .ToList();

            FoodsListBox.ItemsSource = foods;
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LoadFoods(SearchBox.Text);
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (FoodsListBox.SelectedItem != null)
            {
                SelectedFood = FoodsListBox.SelectedItem as Food;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Kérem válasszon egy ételt!", "Figyelmeztetés", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void FoodsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FoodsListBox.SelectedItem != null)
            {
                SelectedFood = FoodsListBox.SelectedItem as Food;
                DialogResult = true;
                Close();
            }
        }
    }
}