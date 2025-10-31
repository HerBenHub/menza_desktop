using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using menza_admin.Models;

namespace menza_admin
{
    /// <summary>
    /// Étel kiválasztó dialógus ablak osztály
    /// Lehetővé teszi a felhasználó számára, hogy válasszon egy ételt a rendelkezésre álló ételek listájából
    /// </summary>
    public partial class FoodSelectionDialog : Window
    {
        private List<Food> _allFoods; // Összes elérhető étel listája
        private List<Food> _excludedFoods; // Kizárt ételek listája (már hozzáadott ételek)

        public Food SelectedFood { get; private set; } // Kiválasztott étel

        /// <summary>
        /// Konstruktor az étel kiválasztó dialógus inicializálásához
        /// </summary>
        /// <param name="allFoods">Az összes elérhető étel listája</param>
        /// <param name="excludedFoods">A kizárt ételek listája</param>
        public FoodSelectionDialog(List<Food> allFoods, List<Food> excludedFoods)
        {
            InitializeComponent();
            _allFoods = allFoods;
            _excludedFoods = excludedFoods;
            LoadFoods();
        }

        /// <summary>
        /// Betölti és szűri az ételeket a keresési szöveg alapján
        /// Kizárja a már hozzáadott ételeket és alkalmazza a keresési feltételeket
        /// </summary>
        /// <param name="searchText">Keresési szöveg (opcionális)</param>
        private void LoadFoods(string searchText = "")
        {
            // Ételek szűrése: kizárjuk a már hozzáadott ételeket és alkalmazzuk a keresési szöveget
            var foods = _allFoods
                .Where(f => !_excludedFoods.Any(ef => ef.Id == f.Id))
                .Where(f => string.IsNullOrEmpty(searchText) || 
                           f.Name.ToLower().Contains(searchText.ToLower()) ||
                           (f.Description != null && f.Description.ToLower().Contains(searchText.ToLower())))
                .OrderBy(f => f.Name)
                .ToList();

            FoodsListBox.ItemsSource = foods;
        }

        /// <summary>
        /// Eseménykezelő a keresőmező szövegváltozásához
        /// Frissíti az ételek listáját a keresési feltételek alapján
        /// </summary>
        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LoadFoods(SearchBox.Text);
        }

        /// <summary>
        /// Eseménykezelő a Kiválaszt gomb kattintásához
        /// Validálja a kiválasztást és bezárja a dialógust
        /// </summary>
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

        /// <summary>
        /// Eseménykezelő a Mégse gomb kattintásához
        /// Bezárja a dialógust kiválasztás nélkül
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Eseménykezelő a lista dupla kattintásához
        /// Gyors kiválasztás és dialógus bezárása
        /// </summary>
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