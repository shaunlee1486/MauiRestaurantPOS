using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;

namespace RestaurantPOS.Controls;

public partial class CategoryListControl : ContentView
{
	// Using a DependencyProperty as the backing store for Categories.  This enables animation, styling, binding, etc...
	public static readonly BindableProperty CategoriesProperty =
        BindableProperty.Create("Categories", typeof(MenuCategoryModel[]), typeof(CategoryListControl), Array.Empty<MenuCategoryModel>());

    public MenuCategoryModel[] Categories 
	{ 
		get => (MenuCategoryModel[])GetValue(CategoriesProperty); 
		set => SetValue(CategoriesProperty, value); 
	}

    public CategoryListControl()
	{
		InitializeComponent();
	}

	public event Action<MenuCategoryModel> OnCategorySelected;

	[RelayCommand]
	private void SelectCategory(MenuCategoryModel category) => OnCategorySelected?.Invoke(category);
}